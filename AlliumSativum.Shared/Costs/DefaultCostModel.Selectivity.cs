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

        var distribution = DistributionUtils.GetDistributionType(attr);

        switch (distribution)
        {
            case QuasiUniformDistributionType quasiUniform:
                return (1.0 / attr.DistinctCardinality) + quasiUniform.CoefficientOfVariation;
            case QuasiConstantDistributionType:
                return (1.0 / attr.DistinctCardinality);
            case PowerLawDistributionType:
            case SkewedDistributionType:
                return Math.Abs(attr.KellySkewness);
        }

        return -1;
    }
    
    private async Task<double> CalculateEquiJoinSelectivityAsync(BinaryOperatorExpressionNode node)
    {
        var leftAttribute = await _catalog.GetAttributeAsync((FullySpecifiedColumnExpressionNode)node.Left);
        var leftRelation = await _catalog.GetRelationAsync(leftAttribute.RelationId);
        var rightAttribute = await _catalog.GetAttributeAsync((FullySpecifiedColumnExpressionNode)node.Right);
        var rightRelation = await _catalog.GetRelationAsync(rightAttribute.RelationId);
        var leftDistribution = DistributionUtils.GetDistributionType(leftAttribute);
        var rightDistribution = DistributionUtils.GetDistributionType(rightAttribute);
        switch (leftDistribution, rightDistribution)
        {
            case (QuasiUniformDistributionType uniLeft, QuasiUniformDistributionType uniRight):
                var selectivity = 1.0 / Math.Max(leftAttribute.DistinctCardinality, rightAttribute.DistinctCardinality);
                var penalty = (uniLeft.CoefficientOfVariation + uniRight.CoefficientOfVariation) / 2;
                        
                return Math.Min(1, selectivity + penalty);
            case (QuasiUniformDistributionType uniform, QuasiConstantDistributionType):
                var uniformSelectivity = (1.0 / leftAttribute.DistinctCardinality) + uniform.CoefficientOfVariation;
                var constantSelectivity = (1.0 / rightAttribute.DistinctCardinality) * _settings.SelectivityEstimation.PenaltyForConstant;

                return uniformSelectivity * constantSelectivity;
            case (QuasiUniformDistributionType, PowerLawDistributionType):
            case (QuasiUniformDistributionType, SkewedDistributionType):
                return Math.Abs(rightAttribute.KellySkewness);
            case (QuasiUniformDistributionType, MultiModalDistributionType):
                return 0.1;
                throw new NotImplementedException();
            case (QuasiConstantDistributionType, QuasiConstantDistributionType):
                return (1.0 / Math.Max(leftAttribute.DistinctCardinality, rightAttribute.DistinctCardinality)) * _settings.SelectivityEstimation.PenaltyForConstant;
            case (QuasiConstantDistributionType, PowerLawDistributionType):
            case (QuasiConstantDistributionType, SkewedDistributionType):
                return (1.0/leftAttribute.DistinctCardinality) * _settings.SelectivityEstimation.PenaltyForConstant * Math.Abs(rightAttribute.KellySkewness);
            case (QuasiConstantDistributionType, MultiModalDistributionType):
                return 0.1;
                throw new NotImplementedException();
            case (PowerLawDistributionType, PowerLawDistributionType):
            case (PowerLawDistributionType, SkewedDistributionType):
            case (SkewedDistributionType, SkewedDistributionType):
                return Math.Max(
                    Math.Abs(leftAttribute.KellySkewness),
                    Math.Abs(rightAttribute.KellySkewness));
            case (PowerLawDistributionType, MultiModalDistributionType):
            case (SkewedDistributionType, MultiModalDistributionType):
                return 0.1;
                throw new NotImplementedException();
            case (MultiModalDistributionType, MultiModalDistributionType):
                return 0.1;
                throw new NotImplementedException();
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
}
