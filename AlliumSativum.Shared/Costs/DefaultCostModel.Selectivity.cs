using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Utils;

namespace AlliumSativum.Shared.Costs;

public partial class DefaultCostModel
{

    private async Task<double> CalculateEqualsSelectivityAsync(BinaryOperatorExpressionNode node)
    {
        var fullySpecified = node.Left as FullySpecifiedColumnExpressionNode ?? (FullySpecifiedColumnExpressionNode)node.Right;
        var attr = await _catalog.GetAttributeAsync(fullySpecified);

        switch (attr.DistributionType)
        {
            case DistributionType.Uniform:
                return (1.0 / attr.DistinctCardinality) + (attr.StandardDeviation / attr.Mean);
            case DistributionType.Constant:
                return (1.0 / attr.DistinctCardinality);
            case DistributionType.PowerLaw:
            case DistributionType.Skewed:
                return Math.Abs(attr.KellySkewness);
        }

        return -1;
    }
    
    private async Task<double> CalculateEquiJoinSelectivityAsync(BinaryOperatorExpressionNode node)
    {
        var leftAttribute = await _catalog.GetAttributeAsync((FullySpecifiedColumnExpressionNode)node.Left);
        var rightAttribute = await _catalog.GetAttributeAsync((FullySpecifiedColumnExpressionNode)node.Right);
        switch (leftAttribute.DistributionType, rightAttribute.DistributionType)
        {
            case (DistributionType.Uniform, DistributionType.Uniform):
                var selectivity = 1.0 / Math.Max(leftAttribute.DistinctCardinality, rightAttribute.DistinctCardinality);
                var penalty = ((leftAttribute.StandardDeviation / leftAttribute.Mean) + (rightAttribute.StandardDeviation / rightAttribute.Mean)) / 2;
                        
                return Math.Min(1, selectivity + penalty);
            case (DistributionType.Uniform, DistributionType.Constant):
                var uniformSelectivity = (1.0 / leftAttribute.DistinctCardinality) + (leftAttribute.StandardDeviation / leftAttribute.Mean);
                var constantSelectivity = (1.0 / rightAttribute.DistinctCardinality) * _settings.SelectivityEstimation.PenaltyForConstant;

                return uniformSelectivity * constantSelectivity;
            case (DistributionType.Uniform, DistributionType.PowerLaw):
            case (DistributionType.Uniform, DistributionType.Skewed):
                return Math.Abs(rightAttribute.KellySkewness);
            case (DistributionType.Uniform, DistributionType.BiModal):
                return 0.1;
                throw new NotImplementedException();
            case (DistributionType.Constant, DistributionType.Constant):
                return (1.0 / Math.Max(leftAttribute.DistinctCardinality, rightAttribute.DistinctCardinality)) * _settings.SelectivityEstimation.PenaltyForConstant;
            case (DistributionType.Constant, DistributionType.PowerLaw):
            case (DistributionType.Constant, DistributionType.Skewed):
                return (1.0/leftAttribute.DistinctCardinality) * _settings.SelectivityEstimation.PenaltyForConstant * Math.Abs(rightAttribute.KellySkewness);
            case (DistributionType.Constant, DistributionType.BiModal):
                return 0.1;
                throw new NotImplementedException();
            case (DistributionType.PowerLaw, DistributionType.PowerLaw):
                if (Math.Sign(leftAttribute.Skewness!.Value) == Math.Sign(rightAttribute.Skewness!.Value))
                {
                    return BinWidth(leftAttribute, rightAttribute) *
                           (Sigmoid(Math.Max(leftAttribute.Skewness!.Value, rightAttribute.Skewness!.Value), 5) / 2 + 1);
                }
                
                return BinWidth(leftAttribute, rightAttribute) *
                       (3 * Sigmoid(Math.Max(leftAttribute.Skewness!.Value, rightAttribute.Skewness!.Value), 5) + 1);
            case (DistributionType.PowerLaw, DistributionType.Skewed):
            case (DistributionType.Skewed, DistributionType.Skewed):
                return Math.Max(
                    Math.Abs(leftAttribute.KellySkewness),
                    Math.Abs(rightAttribute.KellySkewness));
            case (DistributionType.PowerLaw, DistributionType.BiModal):
            case (DistributionType.Skewed, DistributionType.BiModal):
                return 0.1;
                throw new NotImplementedException();
            case (DistributionType.BiModal, DistributionType.BiModal):
                return 0.1;
                throw new NotImplementedException();
            case (DistributionType.Unknown, _):
                return 0.2; // wild guess!
            default:
                // other way, to catch the "other half" of the matrix
                return await CalculateEquiJoinSelectivityAsync(new BinaryOperatorExpressionNode
                {
                    Left = node.Right,
                    Right = node.Left,
                    Operation = node.Operation
                });
        }
    }
    
    private static double Sigmoid(double x, double x0 = 0, double k = 1)
    {
        return 1 / (1 - Math.Exp(-k * (x - x0)));
    }

    private static double BinWidth(AttributeEntity left, AttributeEntity right)
    {
        return 1.0 / Math.Max(left.DistinctCardinality, right.DistinctCardinality);
    }
}
