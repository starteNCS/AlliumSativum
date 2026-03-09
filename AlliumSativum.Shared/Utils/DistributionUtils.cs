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
        
        var modes = FindModes(normalized, attribute);

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

        return Math.Abs(attribute.Skewness.Value) > 1.5 && attribute is { Kurtosis: > 3 };
    }
    
    private static bool IsMultiModal(List<Peak> peaks)
    {
        return peaks.Count > 1;
    }
    
    /// <summary>
    /// Finds all local maxima (modes) in a discrete distribution using Gaussian KDE.
    /// </summary>
    /// <param name="data">Dictionary of Value -> Frequency</param>
    /// <param name="bandwidth">Smoothing factor (higher = smoother, lower = more sensitive)</param>
    /// <param name="threshold">Minimum density required to be considered a 'peak'</param>
    private static List<Peak> FindModes(Dictionary<double, double> data, AttributeEntity attributeEntity, double threshold = 0.01)
    {
        var peaks = new List<Peak>();
        if (data.Count == 0)
        {
            return peaks;
        }
        
        var bandwidth = Math.Pow((4 * Math.Pow(attributeEntity.StandardDeviation, 5)) / (3 * data.Count), 1.0 / 5.0);

        var minX = data.Keys.Min() - (bandwidth * 3);
        var maxX = data.Keys.Max() + (bandwidth * 3);
        var step = bandwidth / 10.0; // Dynamic resolution based on bandwidth

        var lastDensity = CalculateDensity(minX, data, bandwidth);
        var currentDensity = CalculateDensity(minX + step, data, bandwidth);

        for (var x = minX + (2 * step); x <= maxX; x += step)
        {
            var nextDensity = CalculateDensity(x, data, bandwidth);

            // Check for local maximum (Peak: Point is higher than both neighbors)
            if (currentDensity > lastDensity && currentDensity > nextDensity)
            {
                if (currentDensity > threshold)
                {
                    peaks.Add(new Peak { 
                        Location = x - step, 
                        Density = currentDensity 
                    });
                }
            }

            lastDensity = currentDensity;
            currentDensity = nextDensity;
        }

        return peaks;
    }

    private static double CalculateDensity(double x, Dictionary<double, double> data, double sigma)
    {
        double totalDensity = 0;
        foreach (var entry in data)
        {
            var diff = x - entry.Key;
            var exponent = -0.5 * Math.Pow(diff / sigma, 2);
            var gaussian = (1.0 / (sigma * Math.Sqrt(2 * Math.PI))) * Math.Exp(exponent);
            totalDensity += entry.Value * gaussian;
        }
        return totalDensity;
    }
    
    private class Peak
    {
        public double Location { get; set; }
        public double Density { get; set; }
    }
}