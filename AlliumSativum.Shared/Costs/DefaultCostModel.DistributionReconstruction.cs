using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Utils;

namespace AlliumSativum.Shared.Costs;

public sealed partial class DefaultCostModel
{
    public Dictionary<double, double> ReconstructDistribution(PlanOperatorDistributionData distributionData)
    {
        if (distributionData.DistributionType == DistributionType.Uniform)
            return ReconstructUniformDistribution(distributionData);

        return ReconstructGaussDistribution(distributionData);
    }

    private static Dictionary<double, double> ReconstructUniformDistribution(
        PlanOperatorDistributionData distributionData)
    {
        if (distributionData.DistributionType != DistributionType.Uniform)
            throw new ArgumentException(
                $"{distributionData.DistributionType.ToString()} cannot be reconstructed using constant distribution");

        var dictionary = new Dictionary<double, double>();
        if (Math.Abs(distributionData.Min - distributionData.Max) < 0e-3)
        {
            dictionary[distributionData.Min] = distributionData.MeanBinHeight;
            return dictionary;
        }

        for (var i = distributionData.Min; i <= distributionData.Max; i++)
            dictionary[i] = distributionData.MeanBinHeight;

        return dictionary;
    }

    private static Dictionary<double, double> ReconstructGaussDistribution(
        PlanOperatorDistributionData distributionData)
    {
        if (distributionData.DistributionType == DistributionType.Uniform)
            throw new ArgumentException("Constant cannot be reconstructed using Gauss distribution");

        var dictionary = new Dictionary<double, double>();

        for (var i = distributionData.Min; i <= distributionData.Max; i++) dictionary[i] = 0;

        var isSingleBin = Math.Abs(distributionData.Min - distributionData.Max) < 0e-3;
        foreach (var peak in distributionData.Peaks)
        {
            if (isSingleBin)
            {
                var value = NormalizedNormalDistribution(distributionData.Min, peak) * peak.Height;
                if (double.IsNaN(value) || double.IsInfinity(value)) continue;

                if (dictionary[distributionData.Min] < value) dictionary[distributionData.Min] = value;
            }

            for (var i = distributionData.Min; i <= distributionData.Max; i++)
            {
                var value = NormalizedNormalDistribution(i, peak) * peak.Height;
                if (double.IsNaN(value) || double.IsInfinity(value)) continue;

                if (dictionary[i] < value) dictionary[i] = value;
            }
        }

        return dictionary;
    }

    private static double NormalizedNormalDistribution(double x, PlanOperatorDistributionData.Peak peak)
    {
        var first = 1 / Math.Sqrt(2 * Math.PI * Math.Pow(peak.StandardDeviation, 2));
        var exponent = Math.Pow(x - peak.Position, 2) / (2 * Math.Pow(peak.StandardDeviation, 2));
        var maxDensity = 1 / (peak.StandardDeviation * Math.Sqrt(2 * Math.PI));

        var normalDistribution = first * Math.Exp(-exponent);

        return normalDistribution / maxDensity;
    }
}