using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Shared.Utils;

namespace AlliumSativum.Shared.Costs;

public sealed partial class DefaultCostModel
{
    public (Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distribution, double selectivity) GetDistributionOfExpression(BinaryOperatorExpressionNode node, Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData)
    {
        switch (node)
        {
            case { Left: FullySpecifiedColumnExpressionNode, Operation: "=", Right: ValueExpressionNode } or { Right: FullySpecifiedColumnExpressionNode, Operation: "=", Left: ValueExpressionNode }:
                return GetDistributionOfEqualsValueExpression(node, distributionData);
            case { Left: FullySpecifiedColumnExpressionNode, Operation: ">" or "<" or ">=" or "<=", Right: ValueExpressionNode } or { Right: FullySpecifiedColumnExpressionNode, Operation: ">" or "<" or ">=" or "<=", Left: ValueExpressionNode }:
                return GetDistributionOfLessGreaterValueExpression(node, distributionData);
            case { Left: FullySpecifiedColumnExpressionNode, Operation: ">" or "<" or ">=" or "<=", Right: FullySpecifiedColumnExpressionNode }:
            {
                throw new NotImplementedException();
            }
            case { Left: FullySpecifiedColumnExpressionNode, Operation: "=", Right: FullySpecifiedColumnExpressionNode }:
                return GetDistributionOfEqualsAttributeExpression(node, distributionData);
            case {Left : BinaryOperatorExpressionNode, Operation: "OR", Right: BinaryOperatorExpressionNode}:
            {
                var left = GetDistributionOfExpression((BinaryOperatorExpressionNode)node.Left, distributionData);
                var right = GetDistributionOfExpression((BinaryOperatorExpressionNode)node.Right, distributionData);
                return (null!, 0);
            }
            case {Left : BinaryOperatorExpressionNode, Operation: "AND", Right: BinaryOperatorExpressionNode}:
            {
                var left = GetDistributionOfExpression((BinaryOperatorExpressionNode)node.Left, distributionData);
                var right = GetDistributionOfExpression((BinaryOperatorExpressionNode)node.Right, distributionData);
                return (null!, 0);
            }
            default:
                throw new ArgumentException("Unsupported expression node for selectivity estimation");
        }
    }

    private static (Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distribution, double selectivity) GetDistributionOfEqualsAttributeExpression(BinaryOperatorExpressionNode node, Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData)
    {
        var leftAttribute = ((FullySpecifiedColumnExpressionNode)node.Left).Attribute;
        var rightAttribute = ((FullySpecifiedColumnExpressionNode)node.Right).Attribute;
        if(!distributionData.TryGetValue(leftAttribute, out var leftData) || !distributionData.TryGetValue(rightAttribute, out var rightData))
        {
            throw new ArgumentException($"Expected distribution data for attributes {leftAttribute} and {rightAttribute}");
        }

        var previousIntegral = ReconstructDistribution(leftData.CrossJoin(rightData)).Values.Sum();
        
        var joinMin = Math.Max(leftData.Min, rightData.Min);
        var joinMax = Math.Min(leftData.Max, rightData.Max);
        var peaks = leftData.Peaks.Where(peak => peak.Position >= joinMin && peak.Position <= joinMax).ToList();
        
        distributionData[rightAttribute] = distributionData[leftAttribute] = new PlanOperatorDistributionData
        {
            Min = joinMin,
            Max = joinMax,
            Peaks = peaks,
            DistributionType = PeakCountToDistributionType(peaks)
        };

        var nowIntegral = ReconstructDistribution(distributionData[leftAttribute]).Values.Sum();
        var selectivity = nowIntegral / previousIntegral;
        
        distributionData = ScaleDistribution(distributionData, selectivity, skipAttributes: [leftAttribute, rightAttribute]);
        
        return (distributionData, selectivity);
    }

    private static (Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distribution, double selectivity) GetDistributionOfEqualsValueExpression(BinaryOperatorExpressionNode node,
        Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData)
    {
        var attribute = (node.Left as FullySpecifiedColumnExpressionNode ?? (FullySpecifiedColumnExpressionNode)node.Right).Attribute;
        var valueNode = node.Left as ValueExpressionNode ?? (ValueExpressionNode)node.Right;
        if (valueNode.Type != ValueExpressionNode.ValueExpressionType.Numeric)
        {
            // if not a number, we cannot really infer anything about the distribution for now
            // just return as is
            return (distributionData, 1);
        }
        
        var data = distributionData.FirstOrDefault(x => x.Key == attribute).Value;
        if (data is null)
        {
            throw new ArgumentException($"Expected distribution data for attribute {attribute}");
        }
        
        var previousIntegral = ReconstructDistribution(distributionData[attribute]).Values.Sum();

        var value = double.Parse(valueNode.Value);
        var (min, max) = GetValueRange(data, node.Operation, value);
        
        distributionData[attribute] = new PlanOperatorDistributionData
        {
            DistributionType = DistributionType.Constant,
            Min = min,
            Max = max,
            Peaks = []
        };

        var nowIntegral = ReconstructDistribution(distributionData[attribute]).Values.Sum();
        var selectivity = nowIntegral / previousIntegral;
        
        distributionData = ScaleDistribution(distributionData, selectivity, skipAttributes: [attribute]);
        
        return (distributionData, selectivity);
    }

    private static (Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distribution, double selectivity) GetDistributionOfLessGreaterValueExpression(BinaryOperatorExpressionNode node,
        Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData)
    {
        var attribute = (node.Left as FullySpecifiedColumnExpressionNode ?? (FullySpecifiedColumnExpressionNode)node.Right).Attribute;
        var valueNode = node.Left as ValueExpressionNode ?? (ValueExpressionNode)node.Right;
        if (valueNode.Type != ValueExpressionNode.ValueExpressionType.Numeric)
        {
            // if not a number, we cannot really infer anything about the distribution for now
            // just return as is
            return (distributionData, 1);
        }
                
        var value = double.Parse(valueNode.Value);
        var data = distributionData.FirstOrDefault(x => x.Key == attribute).Value;
        if (data is null)
        {
            throw new ArgumentException($"Expected distribution data for attribute {attribute}");
        }
        
        var previousIntegral = ReconstructDistribution(data).Values.Sum();

        var peaksLeft = data.Peaks.Where(peak => CheckPeak(peak, node.Operation, value)).ToList();
        var (min, max) = GetValueRange(data, node.Operation, value);

        distributionData[attribute] = new PlanOperatorDistributionData
        {
            Min = min,
            Max = max,
            Peaks = peaksLeft,
            DistributionType = PeakCountToDistributionType(peaksLeft)
        };
        
        var nowIntegral = ReconstructDistribution(distributionData[attribute]).Values.Sum();
        var selectivity = nowIntegral / previousIntegral;
        
        distributionData = ScaleDistribution(distributionData, selectivity, skipAttributes: [attribute]);

        return (distributionData, selectivity); 
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

    private static Dictionary<AttributeSpecifier, PlanOperatorDistributionData> ScaleDistribution(
        Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData,
        double scalingFactor,
        List<AttributeSpecifier> skipAttributes)
    {
        foreach (var data in distributionData)
        {
            if (skipAttributes.Contains(data.Key))
            {
                continue;
            }
            
            data.Value.Mean *= scalingFactor;
            foreach (var peak in data.Value.Peaks)
            {
                peak.Mean *= scalingFactor;
                peak.Height *= scalingFactor;
            }
        }
        
        return distributionData;
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
