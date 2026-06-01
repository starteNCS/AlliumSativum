using AlliumSativum.Shared.Database.Entities;

namespace AlliumSativum.Shared.Utils;

public enum DistributionType
{
    Constant = 0,
    Uniform = 1,
    PowerLaw = 2,
    UniModal = 3,
    MultiModal = 4,
    Unknown = 5
}

public static class DistributionDetector
{
    /// <summary>
    ///     Calculate the modes of the distribution using Kernel Density Estimation (KDE) and find the peaks in the density
    ///     curve.
    ///     🤖 Developed iteratively with the help of Google Gemini
    /// </summary>
    /// <param name="data">The histogram to find the modes of</param>
    /// <param name="attribute">Attribute base information</param>
    /// <returns>List of modes</returns>
    public static List<AttributePeakEntity> FindModes(Dictionary<double, int> data, AttributeEntity attribute)
    {
        if (data.Count == 0) return [];

        // check if distribution is flat, as KDE places a bell over each datapoint, one peak is guaranteed
        var maxCount = data.Values.Max();
        var minCount = data.Values.Min();
        if (maxCount == minCount || (double)(maxCount - minCount) / maxCount < 0.05) return [];

        double n = data.Values.Sum();

        // Adaptive Bandwidth (Silverman's Rule with a 0.6 sensitivity 'tightener')
        var h = 1.06 * attribute.StandardDeviation * Math.Pow(n, -0.2) * 0.6;

        // Ensure h isn't smaller than the bin resolution to avoid "spiky" artifacts
        var minStep = GetMinDataGap(data);
        h = Math.Max(h, minStep * 1.5);

        var curve = GenerateDensityCurve(data, h, attribute);
        var candidates = GetLocalMaxima(curve);

        if (candidates.Count == 0) return [];

        var maxDensity = candidates.Max(p => p.Density);
        var validPeaks = candidates
            .Where(p => p.Density >= maxDensity * 0.2)
            .OrderByDescending(p => p.Density)
            .ToList();

        var valleys = GetLocalMinima(curve);

        foreach (var peak in validPeaks)
        {
            // closest valley to the left of the peak
            var leftBound = valleys.Where(v => v < peak.Position).DefaultIfEmpty(double.MinValue).Max();
            // closest valley to the right of the peak
            var rightBound = valleys.Where(v => v > peak.Position).DefaultIfEmpty(double.MaxValue).Min();

            var peakData = data.Where(kv => kv.Key >= leftBound && kv.Key <= rightBound);
            (peak.Mean, peak.StandardDeviation) = CalculatePeakStatisticalMeasures(peakData);
        }

        return validPeaks;
    }

    /// <summary>
    ///     Calculate the minimum gap between consecutive data points in the histogram to determine a reasonable bandwidth for
    ///     KDE.
    ///     🤖 Developed iteratively with the help of Google Gemini
    /// </summary>
    /// <param name="data">The histogram</param>
    /// <returns>Minimum gap between two bins</returns>
    private static double GetMinDataGap(Dictionary<double, int> data)
    {
        var sortedKeys = data.Keys.OrderBy(k => k).ToList();
        var minGap = double.MaxValue;
        for (var i = 0; i < sortedKeys.Count - 1; i++)
        {
            var gap = sortedKeys[i + 1] - sortedKeys[i];
            if (gap > 0 && gap < minGap) minGap = gap;
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        return minGap == double.MaxValue ? 1.0 : minGap;
    }

    /// <summary>
    ///     Get density curve using Gaussian Kernel Density Estimation (KDE) for the given data and bandwidth h.
    ///     🤖 Developed iteratively with the help of Google Gemini
    /// </summary>
    /// <param name="data">The histogram</param>
    /// <param name="h">Density configuration parameter</param>
    /// <param name="attribute">Attribute meta information</param>
    /// <returns>Peak candidates</returns>
    private static List<AttributePeakEntity> GenerateDensityCurve(Dictionary<double, int> data, double h,
        AttributeEntity attribute)
    {
        var curve = new List<AttributePeakEntity>();
        var minX = data.Keys.Min() - h * 3;
        var maxX = data.Keys.Max() + h * 3;
        var step = h / 4.0; // Resolution of the scan

        for (var x = minX; x <= maxX; x += step)
        {
            double density = 0;
            foreach (var entry in data)
            {
                var u = (x - entry.Key) / h;
                // Gaussian Kernel
                density += entry.Value * (1.0 / (h * Math.Sqrt(2 * Math.PI))) * Math.Exp(-0.5 * u * u);
            }

            curve.Add(new AttributePeakEntity
            {
                Id = Guid.NewGuid(),
                AttributeId = attribute.Id,
                Position = x,
                Density = density,
                Height = data.OrderBy(kv => Math.Abs(kv.Key - x)).First()
                    .Value // Height is the count of the closest bin
            });
        }

        return curve;
    }

    /// <summary>
    ///     Get local minima from the density curve, i.e. valleys between peaks
    /// </summary>
    /// <param name="curve">Density curve candidates</param>
    /// <returns>Mimimas</returns>
    private static List<double> GetLocalMinima(List<AttributePeakEntity> curve)
    {
        var minima = new List<double>();
        for (var i = 1; i < curve.Count - 1; i++)
            if (curve[i].Density < curve[i - 1].Density && curve[i].Density < curve[i + 1].Density)
                minima.Add(curve[i].Position);

        return minima;
    }


    /// <summary>
    ///     Get local maxima from the density curve, i.e. peaks between hills
    /// </summary>
    /// <param name="curve">Density curve candidates</param>
    /// <returns>Maximas</returns>
    private static List<AttributePeakEntity> GetLocalMaxima(List<AttributePeakEntity> curve)
    {
        var maxima = new List<AttributePeakEntity>();
        for (var i = 1; i < curve.Count - 1; i++)
            if (curve[i].Density > curve[i - 1].Density && curve[i].Density > curve[i + 1].Density)
                maxima.Add(curve[i]);

        return maxima;
    }

    /// <summary>
    ///     Calculate the mean and standard deviation of the data points within a peak
    /// </summary>
    /// <param name="dataSlice"></param>
    /// <returns>The mean and stdev of a peak</returns>
    private static (double mean, double standardDeviation) CalculatePeakStatisticalMeasures(
        IEnumerable<KeyValuePair<double, int>> dataSlice)
    {
        var sliceList = dataSlice.ToList();
        if (sliceList.Count == 0) return (0, 0);

        double totalCount = sliceList.Sum(kv => kv.Value);
        if (totalCount <= 1) return (0, 0);

        var mean = sliceList.Sum(kv => kv.Key * kv.Value) / totalCount;
        var variance = sliceList.Sum(kv => kv.Value * Math.Pow(kv.Key - mean, 2)) / totalCount;

        return (mean, Math.Sqrt(variance));
    }
}