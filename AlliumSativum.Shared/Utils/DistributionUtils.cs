using AlliumSativum.Shared.Database.Entities;

namespace AlliumSativum.Shared.Utils;

public static class DistributionUtils
{
    /// <summary>
    ///     Calculate the distribution of a list of values and update the provided attribute entity with the calculated
    ///     statistics.
    /// </summary>
    /// <param name="values">The raw values</param>
    /// <param name="attribute">The attribute of the values</param>
    /// <returns>Updated attribute and modes of it</returns>
    public static (AttributeEntity attribute, List<AttributePeakEntity> modes) CalculateDistribution(
        List<double?> values, AttributeEntity attribute)
    {
        var nonNullValues = values.Where(x => x.HasValue).Select(x => x.Value).ToList();
        var binnedDistribution = values
            .Select(x => x ?? double.NaN)
            .GroupBy(x => x)
            .OrderBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        if (binnedDistribution.Count == 0) return (attribute, []);

        attribute.Min = nonNullValues.Min();
        attribute.Max = nonNullValues.Max();
        attribute.Mean = nonNullValues.Average();
        attribute.MeanBinHeight = binnedDistribution.Values.Average();
        attribute.Range = nonNullValues.Max() - nonNullValues.Min();
        var variance = 1.0 / nonNullValues.Count *
                       nonNullValues.Select(value => Math.Pow(value - attribute.Mean, 2)).Sum();
        attribute.StandardDeviation = Math.Sqrt(variance);

        var modes = DistributionDetector.FindModes(binnedDistribution, attribute);

        return (attribute, modes);
    }

    public static (AttributeEntity attribute, List<AttributePeakEntity> modes) CalculateDistribution(List<string> data,
        AttributeEntity attribute)
    {
        var frequencies = data.GroupBy(x => x)
            .Select(double? (g) => (double)g.Count())
            .ToList();

        return CalculateDistribution(frequencies, attribute);
    }
}