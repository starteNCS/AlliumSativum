using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Worker.Sdk.Extensions;

public static class ModelExtensions
{
    public static GSelectBaseModel ToGrpcModel(this SelectBaseModel model)
    {
        var payload = new GSelectBaseModel
        {
            From = new GTableSpecifier
            {
                TableName = model.From!.TableName,
                DataSource = model.From!.DataSourceName,
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
        payload.Joins.AddRange(model.Join.Select(j => new GJoinBaseModel()
        {
            Inner = new GTableSpecifier()
            {
                TableName = j.Inner.TableName,
                DataSource = j.Inner.DataSourceName,
            },
            Expression = j.Expression.ToGrpcModel()
        }));

        return payload;
    }

    public static GExpressionNode? ToGrpcModel(this ExpressionNode? root)
    {
        if (root == null)
        {
            return null;
        }

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
                    ValueExpressionNode v => new GExpressionNode { Value = new GValueNode { Value = v.Value } },
                    VariableMappingExpressionNode v => new GExpressionNode { VariableMapping = new GVariableMappingNode { AttributeName = v.VariableMapping.AttributeName, AliasName = v.VariableMapping.VariableName} },
                    FullySpecifiedColumnExpressionNode v => new GExpressionNode { FullySpecified = new GFullySpecifiedColumnNode { DataSourceName = v.Attribute.DataSourceName, TableName =  v.Attribute.TableName, AttributeName = v.Attribute.AttributeName } },
                    _ => throw new System.NotImplementedException()
                };
            }
            stack.Pop();
        }

        return nodeMap[root];
    }
    

    public static SelectBaseModel FromGrpcModel(this GSelectBaseModel model)
    {
        return new SelectBaseModel
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
        if (root is null)
        {
            return null;
        }
        
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
                } ;
            }
            else
            {
                nodeMap[current] = current.NodeTypeCase switch
                {
                    GExpressionNode.NodeTypeOneofCase.Value => new ValueExpressionNode { Value = current.Value.Value },
                    GExpressionNode.NodeTypeOneofCase.FullySpecified => new FullySpecifiedColumnExpressionNode
                    {
                        Attribute = new AttributeSpecifier(current.FullySpecified.DataSourceName, current.FullySpecified.TableName, current.FullySpecified.AttributeName), 
                    },
                    GExpressionNode.NodeTypeOneofCase.VariableMapping => new VariableMappingExpressionNode
                    {
                        VariableMapping = new VariableMappingSpecifier(current.VariableMapping.AliasName, current.VariableMapping.AttributeName)
                    },
                    _ => throw new NotImplementedException()
                };
            }
            stack.Pop();
        }

        return nodeMap[root];
    }
}
