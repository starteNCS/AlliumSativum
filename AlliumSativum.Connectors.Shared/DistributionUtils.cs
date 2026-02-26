using AlliumSativum.Shared.Database.Entities;

namespace AlliumSativum.Connectors.Shared;

public sealed class DistributionUtils
{
    public static AttributeEntity CalculateDistribution(List<double?> values, AttributeEntity attribute)
    {
        if (values.Count == 0)
        {
            return attribute;
        }

        attribute.Mean = values.Average() ?? 0;
        attribute.Range = (values.Max() - values.Min()) ?? 0;
        attribute.Variance = (1.0/values.Count) * values.Select(value => Math.Pow((value - attribute.Mean) ?? 0, 2)).Sum();
        attribute.StandardDeviation = Math.Sqrt(attribute.Variance);
        attribute.Skewness = (1.0/values.Count) * values.Select(value => Math.Pow(
            ((value - attribute.Mean) / attribute.StandardDeviation) ?? 0, 3)).Sum();
        attribute.Kurtosis = (1.0/values.Count) * values.Select(value => Math.Pow(
            ((value - attribute.Mean) / attribute.StandardDeviation) ?? 0, 4)).Sum();
        attribute.KellySkewness = CalculateKellySkewness(values);
        
        return attribute;
    }
    
    public static AttributeEntity CalculateDistribution(List<string> data, AttributeEntity attribute)
    {
        var frequencies = data.GroupBy(x => x)
            .Select(double? (g) => (double)g.Count())
            .ToList();

        return CalculateDistribution(frequencies, attribute);
    }
    
    private static double CalculateKellySkewness(List<double?> data)
    {
        if (data is null || data.Count < 2)
        {
            return 0;
        }

        // not really clean solution, as we just throw away the nulls which also contribute to the skewness theoretically
        var sortedData = data
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .OrderBy(x => x)
            .ToList();

        var p90 = GetPercentile(sortedData, 0.90);
        var p50 = GetPercentile(sortedData, 0.50); // Median
        var p10 = GetPercentile(sortedData, 0.10);

        var denominator = p90 - p10;

        // avoid division by zero if data is uniform
        if (Math.Abs(denominator) < 1e-9)
        {
            return 0;
        }

        return (p90 + p10 - 2 * p50) / denominator;
    }
    
    private static double GetPercentile(List<double> sortedData, double percentile)
    {
        var rank = percentile * (sortedData.Count - 1);
        var index = (int)Math.Floor(rank);
        var fraction = rank - index;

        if (index + 1 < sortedData.Count)
        {
            return sortedData[index] + fraction * (sortedData[index + 1] - sortedData[index]);
        }
        
        return sortedData[index];
    }
}

