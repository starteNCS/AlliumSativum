using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Utils;

namespace AlliumSativum.Connectors.Shared;

public sealed class DistributionUtils
{
    
    // todo: metrics over the bins, not over the values itself, as the values themselves are not really representative of the distribution, but the bins are
    public static (AttributeEntity attribute, List<AttributePeakEntity> modes) CalculateDistribution(List<double?> values, AttributeEntity attribute)
    {
        var binnedDistribution = values
            .Select(x => x ?? double.NaN)
            .GroupBy(x => x)
            .OrderBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.Count());
        
        if (binnedDistribution.Count == 0)
        {
            return (attribute, []);
        }

        attribute.Mean = binnedDistribution.Values.Average();
        attribute.Range = binnedDistribution.Values.Max() - binnedDistribution.Values.Min();
        attribute.Variance = (1.0/binnedDistribution.Values.Count) * binnedDistribution.Values.Select(value => Math.Pow(value - attribute.Mean, 2)).Sum();
        attribute.StandardDeviation = Math.Sqrt(attribute.Variance);

        double n = binnedDistribution.Count;
        attribute.Skewness = attribute.StandardDeviation == 0
            ? null
            : (n / ((n - 1) * (n - 2))) * binnedDistribution.Values
                .Select(value => Math.Pow((value - attribute.Mean) / attribute.StandardDeviation, 3))
                .Sum();
        attribute.Kurtosis = attribute.StandardDeviation == 0
            ? null
            : (n / ((n - 1) * (n - 2))) * binnedDistribution.Values
                .Select(value => Math.Pow((value - attribute.Mean) / attribute.StandardDeviation, 4))
                .Sum();
        (attribute.DistributionType, var modes) = DistributionDetector.Detect(binnedDistribution, attribute);
        
        return (attribute, modes);
    }
    
    public static (AttributeEntity attribute, List<AttributePeakEntity> modes) CalculateDistribution(List<string> data, AttributeEntity attribute)
    {
        var frequencies = data.GroupBy(x => x)
            .Select(double? (g) => (double)g.Count())
            .ToList();

        return CalculateDistribution(frequencies, attribute);
    }
}

