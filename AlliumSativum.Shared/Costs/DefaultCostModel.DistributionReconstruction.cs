using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Utils;

namespace AlliumSativum.Shared.Costs;

public sealed partial class DefaultCostModel
{
    public Dictionary<double, double> ReconstructDistribution(PlanOperatorDistributionData distributionData)
    {
        if (distributionData.DistributionType == DistributionType.Uniform)
        {
            return ReconstructUniformDistribution(distributionData);
        }
        
        return ReconstructGaussDistribution(distributionData);
    }
    
        private static Dictionary<double, double> ReconstructUniformDistribution(PlanOperatorDistributionData distributionData)
    {
        if (distributionData.DistributionType != DistributionType.Uniform)
        {
            throw new ArgumentException($"{distributionData.DistributionType.ToString()} cannot be reconstructed using constant distribution");
        }
        
        var dictionary = new Dictionary<double, double>();
        for (var i = distributionData.Min; i <= distributionData.Max; i++)
        {
            dictionary[i] = distributionData.MeanBinHeight;
        }
        
        return dictionary;
    }
    
    private static Dictionary<double, double> ReconstructGaussDistribution(PlanOperatorDistributionData distributionData)
    {
        if (distributionData.DistributionType == DistributionType.Uniform)
        {
            throw new ArgumentException("Constant cannot be reconstructed using Gauss distribution");
        }
        
        var dictionary = new Dictionary<double, double>();
        for (var i = distributionData.Min; i <= distributionData.Max; i++)
        {
            dictionary[i] = 0;
        }

        foreach (var peak in distributionData.Peaks)
        {
            for (var i = distributionData.Min; i <= distributionData.Max; i++)
            {
                var bell = NormalizedNormalDistribution(i, peak);
                var value = NormalizedNormalDistribution(i, peak) * peak.Height;
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    continue;
                }
                
                if(dictionary[i] < value)
                {
                    dictionary[i] = value;
                }
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
