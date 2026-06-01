using System.Text.Json;
using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.Executor;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AlliumSativum.Worker.Sdk.Extensions;

public static class ModelExtensions
{
    public static GSelectBaseModel ToGrpcModel(this SelectDto model)
    {
        var payload = new GSelectBaseModel
        {
            From = new GTableSpecifier
            {
                TableName = model.From!.TableName,
                DataSource = model.From!.DataSourceName
            },
            Where = model.Where.ToGrpcModel()
        };
        payload.Select.AddRange(model.Select.Select(s =>
        {
            var aSpec = s as AttributeSpecifier;
            return new GAttributeSpecifier
            {
                Table = new GTableSpecifier
                {
                    TableName = aSpec.TableName,
                    DataSource = aSpec.DataSourceName
                },
                AttributeName = aSpec.AttributeName,
                IsHidden = aSpec.IsHidden
            };
        }));
        payload.Joins.AddRange(model.Join.Select(j => new GJoinBaseModel
        {
            Inner = new GTableSpecifier
            {
                TableName = j.Inner.TableName,
                DataSource = j.Inner.DataSourceName
            },
            Expression = j.Expression.ToGrpcModel()
        }));

        return payload;
    }

    public static GExpressionNode? ToGrpcModel(this ExpressionNode? root)
    {
        if (root == null) return null;

        // Map to keep track of processed nodes for reconstruction
        var nodeMap = new Dictionary<ExpressionNode, GExpressionNode>();
        var stack = new Stack<ExpressionNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Peek();

            if (current is BinaryOperatorExpressionNode binary)
            {
                // If children aren't processed yet, push them and continue
                if (!nodeMap.ContainsKey(binary.Left) || !nodeMap.ContainsKey(binary.Right))
                {
                    stack.Push(binary.Right);
                    stack.Push(binary.Left);
                    continue;
                }

                // If children are done, build the current node
                var protoBinary = new GBinaryOperatorNode
                {
                    Operation = binary.Operation,
                    Left = nodeMap[binary.Left],
                    Right = nodeMap[binary.Right]
                };
                nodeMap[current] = new GExpressionNode { BinaryOperator = protoBinary };
            }
            else
            {
                // Leaf Nodes
                nodeMap[current] = current switch
                {
                    ValueExpressionNode v => new GExpressionNode
                        { Value = new GValueNode { Value = v.Value, Type = (int)v.Type } },
                    VariableMappingExpressionNode v => new GExpressionNode
                    {
                        VariableMapping = new GVariableMappingNode
                        {
                            AttributeName = v.VariableMapping.AttributeName, AliasName = v.VariableMapping.VariableName
                        }
                    },
                    FullySpecifiedColumnExpressionNode v => new GExpressionNode
                    {
                        FullySpecified = new GFullySpecifiedColumnNode
                        {
                            DataSourceName = v.Attribute.DataSourceName, TableName = v.Attribute.TableName,
                            AttributeName = v.Attribute.AttributeName
                        }
                    },
                    _ => throw new NotSupportedException()
                };
            }

            stack.Pop();
        }

        return nodeMap[root];
    }

    public static GPlanOperator? ToGrpcModel(this PlanOperator? planOperator)
    {
        if (planOperator is null) return null;

        var pop = planOperator switch
        {
            PushdownSqlPlanOperator psql => new GPlanOperator
            {
                PushdownSql = new GPushdownSqlPlanOperator
                {
                    SqlStatement = psql.SqlStatement,
                    DatasourceId = psql.DataSource.ToString(),
                    Self = new GTableSpecifier
                    {
                        DataSource = psql.Self.DataSourceName,
                        TableName = psql.Self.TableName
                    }
                },
                Cost = planOperator.Cost,
                ExpectedCardinality = planOperator.ExpectedCardinality,
                Width = planOperator.Width
            },
            PushdownRestCallPlanOperator prest => new GPlanOperator
            {
                PushdownRestCall = new GPushdownRestCallPlanOperator
                {
                    DatasourceId = prest.DataSource.ToString(),
                    HttpMethod = prest.HttpMethod,
                    Url = prest.Url,
                    Self = new GTableSpecifier
                    {
                        DataSource = prest.Self.DataSourceName,
                        TableName = prest.Self.TableName
                    }
                },
                Cost = planOperator.Cost,
                ExpectedCardinality = planOperator.ExpectedCardinality,
                Width = planOperator.Width
            },
            _ => new GPlanOperator()
        };

        foreach (var data in planOperator.DistributionData)
        {
            var distribution = new GPlanOperatorDistributionData
            {
                Min = data.Value.Min,
                Max = data.Value.Max,
                Mean = data.Value.Mean,
                MeanBinHeight = data.Value.MeanBinHeight
            };
            distribution.Peaks.Add(data.Value.Peaks.Select(x => new GPlanOperatorDistributionDataPeak
            {
                Position = x.Position,
                Height = x.Height,
                Mean = x.Mean,
                StandardDeviation = x.StandardDeviation
            }));

            pop.OutputDistribution.Add(
                AttributeToString(data.Key),
                distribution);
        }

        return pop;
    }

    public static GExecutorWrapper? ToGrpcModel(this ExecutorWrapper? executor)
    {
        if (executor is null) return null;

        var wrapper = new GExecutorWrapper
        {
            PlanOperator = executor.PlanOperator.ToGrpcModel(),
            FactualCardinality = executor.FactualCardinality,
            FactualCost = executor.FactualCost
        };

        var resultStruct = executor.Result.Select(x =>
        {
            var json = JsonSerializer.Serialize(x);
            return Struct.Parser.ParseJson(json);
        });

        wrapper.Result.Add(resultStruct);

        return wrapper;
    }


    public static SelectDto FromGrpcModel(this GSelectBaseModel model)
    {
        return new SelectDto
        {
            From = new TableSpecifier(model.From.DataSource, model.From.TableName),
            Select = model.Select.Select(ISpecifier (spec) =>
                new AttributeSpecifier(spec.Table.DataSource, spec.Table.TableName, spec.AttributeName)
                {
                    IsHidden = spec.IsHidden
                }).ToList(),
            Where = model.Where.FromGrpcModel(),
            Join = model.Joins.Select(j => new JoinBaseModel
            {
                Expression = j.Expression.FromGrpcModel(),
                Inner = new TableSpecifier(j.Inner.DataSource, j.Inner.TableName),
                JoinType = JoinType.Inner
            }).ToList()
        };
    }

    public static ExpressionNode? FromGrpcModel(this GExpressionNode? root)
    {
        if (root is null) return null;

        var nodeMap = new Dictionary<GExpressionNode, ExpressionNode>();
        var stack = new Stack<GExpressionNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Peek();

            if (current.NodeTypeCase == GExpressionNode.NodeTypeOneofCase.BinaryOperator)
            {
                var bin = current.BinaryOperator;
                if (!nodeMap.ContainsKey(bin.Left) || !nodeMap.ContainsKey(bin.Right))
                {
                    stack.Push(bin.Right);
                    stack.Push(bin.Left);
                    continue;
                }

                nodeMap[current] = new BinaryOperatorExpressionNode
                {
                    Operation = bin.Operation,
                    Left = nodeMap[bin.Left],
                    Right = nodeMap[bin.Right]
                };
            }
            else
            {
                nodeMap[current] = current.NodeTypeCase switch
                {
                    GExpressionNode.NodeTypeOneofCase.Value => new ValueExpressionNode
                    {
                        Value = current.Value.Value, Type = (ValueExpressionNode.ValueExpressionType)current.Value.Type
                    },
                    GExpressionNode.NodeTypeOneofCase.FullySpecified => new FullySpecifiedColumnExpressionNode
                    {
                        Attribute = new AttributeSpecifier(current.FullySpecified.DataSourceName,
                            current.FullySpecified.TableName, current.FullySpecified.AttributeName)
                    },
                    GExpressionNode.NodeTypeOneofCase.VariableMapping => new VariableMappingExpressionNode
                    {
                        VariableMapping = new VariableMappingSpecifier(current.VariableMapping.AliasName,
                            current.VariableMapping.AttributeName)
                    },
                    _ => throw new NotSupportedException()
                };
            }

            stack.Pop();
        }

        return nodeMap[root];
    }

    public static PlanOperator? FromGrpcModel(this GPlanOperator? proto)
    {
        if (proto is null) return null;

        return proto.OperatorTypeCase switch
        {
            GPlanOperator.OperatorTypeOneofCase.PushdownSql => new PushdownSqlPlanOperator(
                Guid.Parse(proto.PushdownSql.DatasourceId),
                proto.PushdownSql.SqlStatement)
            {
                Cost = proto.Cost,
                ExpectedCardinality = proto.ExpectedCardinality,
                Selectivity = 1,
                Self = new TableSpecifier(proto.PushdownSql.Self.DataSource, proto.PushdownSql.Self.TableName),
                DistributionData = proto.OutputDistribution.FromGrpcModel(),
                Width = proto.Width
            },
            GPlanOperator.OperatorTypeOneofCase.PushdownRestCall => new PushdownRestCallPlanOperator(
                Guid.Parse(proto.PushdownRestCall.DatasourceId),
                proto.PushdownRestCall.HttpMethod,
                proto.PushdownRestCall.Url,
                null)
            {
                Cost = proto.Cost,
                ExpectedCardinality = proto.ExpectedCardinality,
                Selectivity = 1,
                Self = new TableSpecifier(proto.PushdownRestCall.Self.DataSource,
                    proto.PushdownRestCall.Self.TableName),
                DistributionData = proto.OutputDistribution.FromGrpcModel(),
                Width = proto.Width
            },
            _ => throw new ArgumentException("Expected some plan operator")
        };
    }

    public static ExecutorWrapper? FromGrpcModel(this GExecutorWrapper? executor)
    {
        if (executor is null) return null;

        var wrapper = new ExecutorWrapper
        {
            PlanOperator = executor.PlanOperator.FromGrpcModel(),
            FactualCardinality = executor.FactualCardinality,
            FactualCost = executor.FactualCost,
            Result = executor.Result.Select(item =>
            {
                var json = item.ToString();
                var jsonElement = JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;
                return jsonElement;
            }).ToList()
        };

        return wrapper;
    }

    public static Dictionary<AttributeSpecifier, PlanOperatorDistributionData> FromGrpcModel(
        this MapField<string, GPlanOperatorDistributionData> distributionData)
    {
        return distributionData.Select(x => new KeyValuePair<AttributeSpecifier, PlanOperatorDistributionData>(
            AttributeFromString(x.Key),
            new PlanOperatorDistributionData
            {
                Min = x.Value.Min,
                Max = x.Value.Max,
                Mean = x.Value.Mean,
                MeanBinHeight = x.Value.MeanBinHeight,
                Peaks = x.Value.Peaks.Select(p => new PlanOperatorDistributionData.Peak
                {
                    Position = p.Position,
                    Height = p.Height,
                    Mean = p.Mean,
                    StandardDeviation = p.StandardDeviation
                }).ToList()
            })).ToDictionary(x => x.Key, x => x.Value);
    }

    private static AttributeSpecifier AttributeFromString(string specifier)
    {
        var firstSplit = specifier.Split(AsSqlParameters.Attribute.DataSourceSeparator);
        var secondSplit = firstSplit[1].Split(AsSqlParameters.Attribute.TableSeparator);
        return new AttributeSpecifier(firstSplit[0], secondSplit[0], secondSplit[1]);
    }

    private static string AttributeToString(AttributeSpecifier specifier)
    {
        return
            $"{specifier.DataSourceName}{AsSqlParameters.Attribute.DataSourceSeparator}{specifier.TableName}{AsSqlParameters.Attribute.TableSeparator}{specifier.AttributeName}";
    }
}