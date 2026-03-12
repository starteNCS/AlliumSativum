using AlliumSativum.Shared.Costs.Models;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Shared.Utils;

namespace AlliumSativum.Shared.Costs;

public sealed partial class DefaultCostModel
{
    public async Task<PlanOperatorDistributionCost> GetDistributionOfExpressionAsync(BinaryOperatorExpressionNode node, Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData, List<PlanOperator> children)
    {
        switch (node)
        {
            case { Left: FullySpecifiedColumnExpressionNode, Operation: "=", Right: ValueExpressionNode } or { Right: FullySpecifiedColumnExpressionNode, Operation: "=", Left: ValueExpressionNode }:
                return await GetDistributionOfEqualsValueExpression(node, distributionData, children);
            case { Left: FullySpecifiedColumnExpressionNode, Operation: ">" or "<" or ">=" or "<=", Right: ValueExpressionNode } or { Right: FullySpecifiedColumnExpressionNode, Operation: ">" or "<" or ">=" or "<=", Left: ValueExpressionNode }:
                return await GetDistributionOfLessGreaterValueExpression(node, distributionData, children);
            case { Left: FullySpecifiedColumnExpressionNode, Operation: ">" or "<" or ">=" or "<=", Right: FullySpecifiedColumnExpressionNode }:
            {
                throw new NotImplementedException();
            }
            case { Left: FullySpecifiedColumnExpressionNode, Operation: "=", Right: FullySpecifiedColumnExpressionNode }:
                return await GetDistributionOfEqualsAttributeExpression(node, distributionData, children);
            case {Left : BinaryOperatorExpressionNode, Operation: "OR", Right: BinaryOperatorExpressionNode}:
                return await GetDistributionsOfOrExpression(node, distributionData, children);
            case {Left : BinaryOperatorExpressionNode, Operation: "AND", Right: BinaryOperatorExpressionNode}:
                return await GetDistributionsOfAndExpression(node, distributionData, children);
            default:
                throw new ArgumentException("Unsupported expression node for selectivity estimation");
        }
    }

    private async Task<PlanOperatorDistributionCost> GetDistributionsOfAndExpression(BinaryOperatorExpressionNode node, Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData,
        List<PlanOperator> children)
    {
        var left = await GetDistributionOfExpressionAsync((BinaryOperatorExpressionNode)node.Left, distributionData, children);
        var right = await GetDistributionOfExpressionAsync((BinaryOperatorExpressionNode)node.Right, distributionData, children);
        
        
        var newSelectivity = left.Selectivity * right.Selectivity; // from selinger optimizer
        return new PlanOperatorDistributionCost
        {
            Distribution = AndMergeDistributions(left.Distribution, right.Distribution),
            Selectivity = newSelectivity,
            Cardinality = (long)((left.Cardinality + right.Cardinality) * newSelectivity)
        };
    }

    private static Dictionary<AttributeSpecifier, PlanOperatorDistributionData> AndMergeDistributions(Dictionary<AttributeSpecifier, PlanOperatorDistributionData> left, Dictionary<AttributeSpecifier, PlanOperatorDistributionData> right)
    {
        var newDistribution = new Dictionary<AttributeSpecifier, PlanOperatorDistributionData>(left);
        foreach (var distri in right)
        {
            if (newDistribution.TryGetValue(distri.Key, out var existingData))
            {
                var min = Math.Max(existingData.Min, distri.Value.Min);
                var max = Math.Min(existingData.Max, distri.Value.Max);
                var peaks = ((List<PlanOperatorDistributionData.Peak>)[..existingData.Peaks, ..distri.Value.Peaks])
                    .Where(peak => peak.Position >= min && peak.Position <= max)
                    .ToList();

                newDistribution[distri.Key] = new PlanOperatorDistributionData
                {
                    DistributionType = PeakCountToDistributionType(peaks),
                    Min = min,
                    Max = max,
                    Peaks = peaks,
                    Mean = (existingData.Mean + distri.Value.Mean) / 2,
                    MeanBinHeight = (existingData.MeanBinHeight + distri.Value.MeanBinHeight) / 2
                };
                continue;
            }
            
            newDistribution[distri.Key] = distri.Value;
        }
        
        return newDistribution;
    }

    private async Task<PlanOperatorDistributionCost> GetDistributionsOfOrExpression(BinaryOperatorExpressionNode node, Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData,
        List<PlanOperator> children)
    {
        var left = await GetDistributionOfExpressionAsync((BinaryOperatorExpressionNode)node.Left, distributionData, children);
        var right = await GetDistributionOfExpressionAsync((BinaryOperatorExpressionNode)node.Right, distributionData, children);

        var newSelectivity = left.Selectivity + right.Selectivity - left.Selectivity * right.Selectivity; // from selinger optimizer
        return new PlanOperatorDistributionCost
        {
            Distribution = OrMergeDistributions(left.Distribution, right.Distribution),
            Selectivity = newSelectivity,
            Cardinality = (long)((left.Cardinality + right.Cardinality) * newSelectivity)
        };
    }
    
    private static Dictionary<AttributeSpecifier, PlanOperatorDistributionData> OrMergeDistributions(Dictionary<AttributeSpecifier, PlanOperatorDistributionData> left, Dictionary<AttributeSpecifier, PlanOperatorDistributionData> right)
    {
        var newDistribution = new Dictionary<AttributeSpecifier, PlanOperatorDistributionData>(left);
        foreach (var distri in right)
        {
            if (newDistribution.TryGetValue(distri.Key, out var existingData))
            {
                var min = Math.Min(existingData.Min, distri.Value.Min);
                var max = Math.Max(existingData.Max, distri.Value.Max);
                List<PlanOperatorDistributionData.Peak> peaks = [..existingData.Peaks, ..distri.Value.Peaks];

                newDistribution[distri.Key] = new PlanOperatorDistributionData
                {
                    DistributionType = PeakCountToDistributionType(peaks),
                    Min = min,
                    Max = max,
                    Peaks = peaks,
                    Mean = (existingData.Mean + distri.Value.Mean) / 2,
                    MeanBinHeight = (existingData.MeanBinHeight + distri.Value.MeanBinHeight) / 2
                };
                continue;
            }
            
            newDistribution[distri.Key] = distri.Value;
        }

        return newDistribution;
    }

    private async Task<PlanOperatorDistributionCost> GetDistributionOfEqualsAttributeExpression(BinaryOperatorExpressionNode node, Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData, List<PlanOperator> children)
    {
        var leftAttribute = ((FullySpecifiedColumnExpressionNode)node.Left).Attribute;
        var rightAttribute = ((FullySpecifiedColumnExpressionNode)node.Right).Attribute;
        if(!distributionData.TryGetValue(leftAttribute, out var leftData) || !distributionData.TryGetValue(rightAttribute, out var rightData))
        {
            throw new ArgumentException($"Expected distribution data for attributes {leftAttribute} and {rightAttribute}");
        }

        var leftRelation = await _catalog.GetRelationAsync(leftAttribute.DataSourceName, leftAttribute.TableName);
        var rightRelation = await _catalog.GetRelationAsync(rightAttribute.DataSourceName, rightAttribute.TableName);

        var crossJoinCount = leftRelation.Cardinality * rightRelation.Cardinality;
        
        var joinMin = Math.Max(leftData.Min, rightData.Min);
        var joinMax = Math.Min(leftData.Max, rightData.Max);
        var peaks = ((List<PlanOperatorDistributionData.Peak>)[..leftData.Peaks, ..rightData.Peaks])
            .Where(peak => peak.Position >= joinMin && peak.Position <= joinMax)
            .ToList();
        
        distributionData[rightAttribute] = distributionData[leftAttribute] = new PlanOperatorDistributionData
        {
            Min = joinMin,
            Max = joinMax,
            Peaks = peaks,
            MeanBinHeight = GetMeanBinHeight([leftData, rightData]),
            DistributionType = PeakCountToDistributionType(peaks)
        };

        var distribution = ReconstructDistribution(distributionData[leftAttribute]);
        var nowIntegral = distribution.Values.Sum();
        var selectivity = Math.Min(1, nowIntegral / crossJoinCount);

        var childrenCrossJoinCount = children.Select(x => x.ExpectedCardinality).Aggregate((a, b) => a * b);
        var cardinality = (long)(childrenCrossJoinCount * selectivity);
        return new PlanOperatorDistributionCost
        {
            Distribution = distributionData,
            Selectivity = selectivity,
            Cardinality = cardinality
        };
    }

    private async Task<PlanOperatorDistributionCost> GetDistributionOfEqualsValueExpression(BinaryOperatorExpressionNode node,
        Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData, 
        List<PlanOperator> children)
    {
        var attribute = (node.Left as FullySpecifiedColumnExpressionNode ?? (FullySpecifiedColumnExpressionNode)node.Right).Attribute;
        var relation = await _catalog.GetRelationAsync(attribute.DataSourceName, attribute.TableName);
        var valueNode = node.Left as ValueExpressionNode ?? (ValueExpressionNode)node.Right;
        if (valueNode.Type != ValueExpressionNode.ValueExpressionType.Numeric)
        {
            // if not a number, we cannot really infer anything about the distribution for now
            // just return as is
            return new PlanOperatorDistributionCost
            {
                Distribution = distributionData,
                Selectivity = 1,
                Cardinality = relation.Cardinality
            };
        }
        
        if (!distributionData.TryGetValue(attribute, out var data))
        {
            throw new ArgumentException($"Expected distribution data for attribute {attribute}");
        }
        
        var value = double.Parse(valueNode.Value);
        var (min, max) = GetValueRange(data, node.Operation, value);
        
        distributionData[attribute] = new PlanOperatorDistributionData
        {
            DistributionType = DistributionType.Uniform,
            Min = min,
            Max = max,
            MeanBinHeight = GetMeanBinHeight([distributionData[attribute]]), // TODO
            Peaks = []
        };

        var nowIntegral = ReconstructDistribution(distributionData[attribute]).Values.Sum();
        var selectivity = Math.Min(1, nowIntegral / relation.Cardinality);
        

        return new PlanOperatorDistributionCost
        {
            Distribution = distributionData,
            Selectivity = selectivity,
            Cardinality = (long)(children.Single().ExpectedCardinality * selectivity)
        };
    }

    private async Task<PlanOperatorDistributionCost> GetDistributionOfLessGreaterValueExpression(BinaryOperatorExpressionNode node,
        Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData, 
        List<PlanOperator> children)
    {
        var attribute = (node.Left as FullySpecifiedColumnExpressionNode ?? (FullySpecifiedColumnExpressionNode)node.Right).Attribute;
        var relation = await _catalog.GetRelationAsync(attribute.DataSourceName, attribute.TableName);
        var valueNode = node.Left as ValueExpressionNode ?? (ValueExpressionNode)node.Right;
        if (valueNode.Type != ValueExpressionNode.ValueExpressionType.Numeric)
        {
            // if not a number, we cannot really infer anything about the distribution for now
            // just return as is
            return new PlanOperatorDistributionCost
            {
                Distribution = distributionData,
                Selectivity = 1,
                Cardinality = relation.Cardinality
            };
        }
                
        var value = double.Parse(valueNode.Value);
        if (!distributionData.TryGetValue(attribute, out var data))
        {
            throw new ArgumentException($"Expected distribution data for attribute {attribute}");
        }
        

        var peaksLeft = data.Peaks.Where(peak => CheckPeak(peak, node.Operation, value)).ToList();
        var (min, max) = GetValueRange(data, node.Operation, value);

        distributionData[attribute] = new PlanOperatorDistributionData
        {
            Min = min,
            Max = max,
            Peaks = peaksLeft,
            MeanBinHeight = GetMeanBinHeight([distributionData[attribute]]), 
            DistributionType = PeakCountToDistributionType(peaksLeft)
        };

        var nowIntegral = ReconstructDistribution(distributionData[attribute]).Values.Sum();
        
        // clamp to 1, as the nowIntegral may be higher than the relation (happens especially for skewed and/or sparse data)
        var selectivity = Math.Min(1, nowIntegral / relation.Cardinality);


        return new PlanOperatorDistributionCost
        {
            Distribution = distributionData,
            Selectivity = selectivity,
            Cardinality = (long)(children.Single().ExpectedCardinality * selectivity)
        };
    }

    private double GetMeanBinHeight(List<PlanOperatorDistributionData> distributionData)
    {
        var heights = 0.0;
        var bins = 0;
        foreach (var data in distributionData)
        {
            var reconstruction = ReconstructDistribution(data);
            bins += reconstruction.Count;
            heights += reconstruction.Values.Sum();
        }
        
        return bins == 0 ? 0 : heights / bins;
    }
    
    private static DistributionType PeakCountToDistributionType(List<PlanOperatorDistributionData.Peak> peaksLeft)
    {
        return peaksLeft.Count switch
        {
            0 => DistributionType.Uniform,
            1 => DistributionType.UniModal,
            > 1 => DistributionType.MultiModal,
            _ => DistributionType.Unknown
        };
    }
    
    private static (double min, double max) GetValueRange(PlanOperatorDistributionData data, string operation, double value)
    {
        if (operation == ">")
        {
            var min = value + 1;
            return min > data.Max 
                ? (double.NaN, double.NaN) 
                : (min, data.Max);
        }

        if (operation == ">=")
        {
            var min = value;
            return min > data.Max 
                ? (double.NaN, double.NaN) 
                : (min, data.Max);
        }

        if (operation == "=")
        {
            return (value, value);
        }

        if (operation == "<=")
        {
            var max = value;
            return max < data.Min
                ? (double.NaN, double.NaN) 
                : (data.Min, value);
        }

        if (operation == "<")
        {
            var max = value - 1;
            return max < data.Min
                ? (double.NaN, double.NaN) 
                : (data.Min, value);
        }

        return (double.NaN, double.NaN);
    }
    
    private static bool CheckPeak(PlanOperatorDistributionData.Peak peak, string operation, double value)
    {
        return operation switch
        {
            "=" => Math.Abs(peak.Position - value) < 0e-9,
            ">" => peak.Position > value,
            "<" => peak.Position < value,
            ">=" => peak.Position >= value,
            "<=" => peak.Position <= value,
            _ => throw new ArgumentException($"Unsupported operation {operation}")
        };
    }
}
