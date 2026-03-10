using AlliumSativum.Shared.Database.Entities;
using MathNet.Numerics.Distributions;

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
    public static (DistributionType distributionType, List<AttributePeakEntity> peaks) Detect(Dictionary<double, int> orderedBinnedValues, AttributeEntity attribute)
    {
        var normalized = Normalize(orderedBinnedValues);
        var modes = FindModes(orderedBinnedValues, attribute);

        if (IsConstant(normalized, modes))
        {
            return (DistributionType.Constant, modes);
        }

        if (IsPowerLaw(normalized, modes))
        {
            return (DistributionType.PowerLaw, modes);
        }

        if (IsUniModal(modes))
        {
            return (DistributionType.UniModal, modes);
        }
        
        if (IsMultiModal(modes, attribute))
        {
            return (DistributionType.MultiModal, modes);
        }
        
        return (DistributionType.Uniform, []);
    }
    
    /// <summary>
    /// Normalize the values to sum to 1
    /// Each bin is represented by the fraction of items it holds
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    private static Dictionary<double, double> Normalize(Dictionary<double, int> values)
    {
        double total = values.Sum(x => x.Value);
        return values
            .Select(x => new
            {
                Key = x.Key,
                Value = x.Value / total
            }).ToDictionary(x => x.Key, x => x.Value);
    }

    /// <summary>
    /// Quasi constant, if one bin has more than 90% of the items 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    private static bool IsConstant(Dictionary<double, double> values, List<AttributePeakEntity> modes)
    {
        return modes.Count == 1 && values.Any(x => x.Value > 0.9);
    }

    /// <summary>
    /// First bin has most items, all other bins have less (within a certain degree or error)
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    private static bool IsPowerLaw(Dictionary<double, double> values, List<AttributePeakEntity> modes, bool reverse = false)
    {
        if (modes.Count != 1)
        {
            return false;
        }
        
        var items = values.Values.ToList();
        if (reverse)
        {
            items = [..items.ToList()];
            items.Reverse();
        }
        
        if (values.First().Value < 0.4)
        {
            return reverse ? false : IsPowerLaw(values, modes, reverse: true);
        }

        var wasPreviousHigher = false;
        var previous = items.First();
        foreach (var value in items.Skip(1))
        {
            // the next item may be a little higher than the previous one, but if it is way more than 10% higher, then it is not a power law distribution
            if (value > previous * 1.1)
            {
                return reverse ? false : IsPowerLaw(values, modes, reverse: true);
            }

            if (value > previous && value < previous * 1.1)
            {
                if (wasPreviousHigher)
                {
                    // if we have already seen a value that is higher than the previous one, and we see another one, then it is not a power law distribution
                    return reverse ? false : IsPowerLaw(values, modes, reverse: true);
                }
                wasPreviousHigher = true;
            }
            else
            {
                wasPreviousHigher = false;
            }

            previous = value;
        }

        // if we've seen all items and all checks passed - it is powerlaw
        return true;
    }

    private static bool IsUniModal(List<AttributePeakEntity> peaks)
    {
        return peaks.Count == 1;
    }
    
    private static bool IsMultiModal(List<AttributePeakEntity> peaks, AttributeEntity attribute)
    {
        var coefficientOfVariance = attribute.StandardDeviation / attribute.Mean;
        var peakRatio = (double) peaks.Count / attribute.DistinctCardinality;
        return peaks.Count > 1 && coefficientOfVariance > 0.2 && peakRatio < 0.15;
    }
    
    private static List<AttributePeakEntity> FindModes(Dictionary<double, int> data, AttributeEntity attribute)
    {
        if (data.Count == 0)
        {
            return [];
        }

        double n = data.Values.Sum();

        if (attribute.StandardDeviation <= 0e-6)
        {
            // All data points are the same
            return [];
        }

        // Adaptive Bandwidth (Silverman's Rule with a 0.6 sensitivity 'tightener')
        double h = (1.06 * attribute.StandardDeviation * Math.Pow(n, -0.2)) * 0.6;

        // Ensure h isn't smaller than the bin resolution to avoid "spiky" artifacts
        double minStep = GetMinDataGap(data);
        h = Math.Max(h, minStep * 1.5);

        var curve = GenerateDensityCurve(data, h, attribute);
        var candidates = GetLocalMaxima(curve);

        // We only keep peaks that are at least 20% as tall as the absolute highest peak.
        if (candidates.Count == 0)
        {
            return [];
        }
        var maxDensity = candidates.Max(p => p.Density);
        
        return candidates
            .Where(p => p.Density >= maxDensity * 0.2)
            .OrderByDescending(p => p.Density)
            .ToList();
    }

    private static double GetMinDataGap(Dictionary<double, int> data)
    {
        var sortedKeys = data.Keys.OrderBy(k => k).ToList();
        var minGap = double.MaxValue;
        for (int i = 0; i < sortedKeys.Count - 1; i++)
        {
            var gap = sortedKeys[i + 1] - sortedKeys[i];
            if (gap > 0 && gap < minGap)
            {
                minGap = gap;
            }
        }
        return minGap == double.MaxValue ? 1.0 : minGap;
    }

    private static List<AttributePeakEntity> GenerateDensityCurve(Dictionary<double, int> data, double h, AttributeEntity attribute)
    {
        var curve = new List<AttributePeakEntity>();
        var minX = data.Keys.Min() - (h * 3);
        var maxX = data.Keys.Max() + (h * 3);
        var step = h / 4.0; // Resolution of the scan

        for (var x = minX; x <= maxX; x += step)
        {
            double density = 0;
            foreach (var entry in data)
            {
                double u = (x - entry.Key) / h;
                // Gaussian Kernel
                density += entry.Value * (1.0 / (h * Math.Sqrt(2 * Math.PI))) * Math.Exp(-0.5 * u * u);
            }
            curve.Add(new AttributePeakEntity
            {
                Id = Guid.NewGuid(),
                AttributeId = attribute.Id,
                Position = x, 
                Density = density,
                Height = data.OrderBy(kv => Math.Abs(kv.Key - x)).First().Value // Height is the count of the closest bin
            });
        }
        return curve;
    }

    private static List<AttributePeakEntity> GetLocalMaxima(List<AttributePeakEntity> curve)
    {
        var maxima = new List<AttributePeakEntity>();
        for (int i = 1; i < curve.Count - 1; i++)
        {
            if (curve[i].Density > curve[i - 1].Density && curve[i].Density > curve[i + 1].Density)
            {
                maxima.Add(curve[i]);
            }
        }
        return maxima;
    }
}