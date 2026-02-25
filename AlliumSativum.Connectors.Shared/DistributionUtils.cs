using AlliumSativum.Shared.Database.Entities;

namespace AlliumSativum.Connectors.Shared;

public sealed class DistributionUtils
{
    public static AttributeEntity CalculateDistribution(List<double> values, AttributeEntity attribute)
    {
        if (values.Count == 0)
        {
            return attribute;
        }

        attribute.Mean = values.Average();
        attribute.Range = values.Max() - values.Min();
        attribute.Variance = (1.0/values.Count) * values.Select(value => Math.Pow(value - attribute.Mean, 2)).Sum();
        attribute.StandardDeviation = Math.Sqrt(attribute.Variance);
        attribute.Skewness = (1.0/values.Count) * values.Select(value => Math.Pow((value - attribute.Mean) / attribute.StandardDeviation, 3)).Sum();
        attribute.Kurtosis = (1.0/values.Count) * values.Select(value => Math.Pow((value - attribute.Mean) / attribute.StandardDeviation, 4)).Sum();

        return attribute;
    }
    
    public static AttributeEntity CalculateDistribution(List<string> data, AttributeEntity attribute)
    {
        var frequencies = data.GroupBy(x => x)
            .Select(g => (double)g.Count())
            .ToList();

        return CalculateDistribution(frequencies, attribute);
    }
}

