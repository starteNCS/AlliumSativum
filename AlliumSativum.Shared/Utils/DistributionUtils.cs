using AlliumSativum.Shared.Database.Entities;
using MathNet.Numerics.Distributions;

namespace AlliumSativum.Shared.Utils;

public enum DistributionType
{
    Constant,
    Uniform,
    PowerLaw,
    Skewed,
    UniModal,
    MultiModal,
    Unknown
}

public static class DistributionDetector
{
    public static DistributionType Detect(Dictionary<double, int> orderedBinnedValues, AttributeEntity attribute)
    {
        var normalized = Normalize(orderedBinnedValues);

        if (IsConstant(normalized))
        {
            return DistributionType.Constant;
        }

        if (IsPowerLaw(normalized))
        {
            return DistributionType.PowerLaw;
        }
        
        var modes = FindModes(orderedBinnedValues, attribute);

        if (IsSkewed(modes, attribute))
        {
            return DistributionType.Skewed;
        }
        
        if (IsMultiModal(modes))
        {
            return DistributionType.MultiModal;
        }
        
        return DistributionType.Uniform;
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
    private static bool IsConstant(Dictionary<double, double> values)
    {
        return values.Any(x => x.Value > 0.9);
    }

    /// <summary>
    /// First bin has most items, all other bins have less (within a certain degree or error)
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    private static bool IsPowerLaw(Dictionary<double, double> values)
    {
        if (values.First().Value < 0.4)
        {
            return false;
        }

        var wasPreviousHigher = false;
        var previous = values.First().Value;
        foreach (var value in values.Skip(1).Select(x => x.Value))
        {
            // the next item may be a little higher than the previous one, but if it is way more than 10% higher, then it is not a power law distribution
            if (value > previous * 1.1)
            {
                return false;
            }

            if (value > previous && value < previous * 1.1)
            {
                if (wasPreviousHigher)
                {
                    // if we have already seen a value that is higher than the previous one, and we see another one, then it is not a power law distribution
                    return false;
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

    private static bool IsSkewed(List<Peak> peaks, AttributeEntity attribute)
    {
        if (peaks.Count != 1 || attribute.Skewness is null)
        {
            return false;
        }

        return Math.Abs(attribute.Skewness.Value) > 0.5;
    }
    
    private static bool IsMultiModal(List<Peak> peaks)
    {
        return peaks.Count > 1;
    }
    
    private static List<Peak> FindModes(Dictionary<double, int> data, AttributeEntity attribute)
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

        var curve = GenerateDensityCurve(data, h);
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

    private static List<Peak> GenerateDensityCurve(Dictionary<double, int> data, double h)
    {
        var curve = new List<Peak>();
        double minX = data.Keys.Min() - (h * 3);
        double maxX = data.Keys.Max() + (h * 3);
        double step = h / 4.0; // Resolution of the scan

        for (double x = minX; x <= maxX; x += step)
        {
            double density = 0;
            foreach (var entry in data)
            {
                double u = (x - entry.Key) / h;
                // Gaussian Kernel
                density += entry.Value * (1.0 / (h * Math.Sqrt(2 * Math.PI))) * Math.Exp(-0.5 * u * u);
            }
            curve.Add(new Peak { Location = x, Density = density });
        }
        return curve;
    }

    private static List<Peak> GetLocalMaxima(List<Peak> curve)
    {
        var maxima = new List<Peak>();
        for (int i = 1; i < curve.Count - 1; i++)
        {
            if (curve[i].Density > curve[i - 1].Density && curve[i].Density > curve[i + 1].Density)
            {
                maxima.Add(curve[i]);
            }
        }
        return maxima;
    }
    
    private class Peak
    {
        public double Location { get; set; }
        public double Density { get; set; }
    }
}