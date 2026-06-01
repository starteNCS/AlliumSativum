using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;

namespace AlliumSativum.Shared.Costs;

public sealed partial class DefaultCostModel
{
    /// <inheritdoc />
    public Dictionary<double, double> ReconstructDistribution(PlanOperatorDistributionData distributionData)
    {
        if (distributionData.Peaks.Count == 0)
            return ReconstructUniformDistribution(distributionData);

        return ReconstructGaussDistribution(distributionData);
    }

    /// <summary>
    ///     Reconstructing a uniform distribution from the given distribution data,
    ///     by filling the histogram with the mean bin height for each bin
    /// </summary>
    /// <param name="distributionData">The attributes distribution data</param>
    /// <returns>The histogram</returns>
    /// <exception cref="ArgumentException">Distribution type was not uniform</exception>
    private static Dictionary<double, double> ReconstructUniformDistribution(
        PlanOperatorDistributionData distributionData)
    {
        if (distributionData.Peaks.Count != 0)
            throw new ArgumentException(
                $"Peak count = {distributionData.Peaks.Count} cannot be reconstructed using constant distribution");

        var dictionary = new Dictionary<double, double>();
        if (Math.Abs(distributionData.Min - distributionData.Max) < 0e-3)
        {
            dictionary[distributionData.Min] = distributionData.MeanBinHeight;
            return dictionary;
        }

        for (var i = distributionData.Min; i <= distributionData.Max; i++)
            dictionary[i] = distributionData.MeanBinHeight;

        return dictionary;
    }

    /// <summary>
    ///     Reconstructing any other distribution using multiple overlapping Gaussians
    /// </summary>
    /// <param name="distributionData">The attribtue distribution data</param>
    /// <returns>The histogram</returns>
    /// <exception cref="ArgumentException">Distribute type was uniform</exception>
    private static Dictionary<double, double> ReconstructGaussDistribution(
        PlanOperatorDistributionData distributionData)
    {
        if (distributionData.Peaks.Count == 0)
            throw new ArgumentException("Uniform cannot be reconstructed using Gauss distribution");

        var histogram = new Dictionary<double, double>();

        for (var i = distributionData.Min; i <= distributionData.Max; i++) histogram[i] = 0;

        var isSingleBin = Math.Abs(distributionData.Min - distributionData.Max) < 0e-3;
        foreach (var peak in distributionData.Peaks)
            HandlePeakForReconstruction(distributionData, isSingleBin, peak, histogram);

        return histogram;
    }

    /// <summary>
    ///     Calculates the contribution of a single peak to the histogram, and updates the histogram values if the contribution
    ///     is higher than the current value
    /// </summary>
    /// <param name="distributionData">The distributions data</param>
    /// <param name="isSingleBin">If the histogram consists of only one bin</param>
    /// <param name="peak">The current peak to calcluate for</param>
    /// <param name="histogram">The histogram</param>
    private static void HandlePeakForReconstruction(PlanOperatorDistributionData distributionData, bool isSingleBin,
        PlanOperatorDistributionData.Peak peak, Dictionary<double, double> histogram)
    {
        if (isSingleBin)
        {
            var value = NormalizedNormalDistribution(distributionData.Min, peak) * peak.Height;
            if (double.IsNaN(value) || double.IsInfinity(value)) return;

            if (histogram[distributionData.Min] < value) histogram[distributionData.Min] = value;
        }

        for (var i = distributionData.Min; i <= distributionData.Max; i++)
        {
            var value = NormalizedNormalDistribution(i, peak) * peak.Height;
            if (double.IsNaN(value) || double.IsInfinity(value)) continue;

            if (histogram[i] < value) histogram[i] = value;
        }
    }

    /// <summary>
    ///     Calculates the normalized normal distribution value for the given position and peak parameters, so that the maximum
    ///     value of the distribution is 1
    /// </summary>
    /// <param name="x">The position to calculate for</param>
    /// <param name="peak">The current peak to calculate for</param>
    /// <returns>The height at position x</returns>
    private static double NormalizedNormalDistribution(double x, PlanOperatorDistributionData.Peak peak)
    {
        var first = 1 / Math.Sqrt(2 * Math.PI * Math.Pow(peak.StandardDeviation, 2));
        var exponent = Math.Pow(x - peak.Position, 2) / (2 * Math.Pow(peak.StandardDeviation, 2));
        var maxDensity = 1 / (peak.StandardDeviation * Math.Sqrt(2 * Math.PI));

        var normalDistribution = first * Math.Exp(-exponent);

        return normalDistribution / maxDensity;
    }
}