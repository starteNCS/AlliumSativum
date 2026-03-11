using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Utils;

namespace AlliumSativum.Shared.Costs;

public sealed partial class DefaultCostModel
{
    private static Dictionary<double, double> ReconstructDistribution(PlanOperatorDistributionData distributionData)
    {
        if (distributionData.DistributionType == DistributionType.Constant)
        {
            return ReconstructConstantDistribution(distributionData);
        }
        
        return ReconstructGaussDistribution(distributionData);
    }
    
        private static Dictionary<double, double> ReconstructConstantDistribution(PlanOperatorDistributionData distributionData)
    {
        if (distributionData.DistributionType != DistributionType.Constant)
        {
            throw new ArgumentException($"{distributionData.DistributionType.ToString()} cannot be reconstructed using constant distribution");
        }
        
        var dictionary = new Dictionary<double, double>();
        for (var i = distributionData.Min; i <= distributionData.Max; i++)
        {
            dictionary[i] = distributionData.Mean;
        }
        
        return dictionary;
    }
    
    private static Dictionary<double, double> ReconstructGaussDistribution(PlanOperatorDistributionData distributionData)
    {
        if (distributionData.DistributionType == DistributionType.Constant)
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
                var value = GaussBell(i, peak) * peak.Height;
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

    private static double GaussBell(double x, PlanOperatorDistributionData.Peak peak)
    {
        var first = 1 / (peak.StandardDeviation * Math.Sqrt(2 * Math.PI));
        var exponent = -0.5 * (Math.Pow((x - peak.Mean) / peak.StandardDeviation, 2) / Math.Pow(peak.StandardDeviation, 2));
        return first * Math.Exp(exponent);
    }
}
