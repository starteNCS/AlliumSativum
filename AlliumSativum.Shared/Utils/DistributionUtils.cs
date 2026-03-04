using AlliumSativum.Shared.Database.Entities;
using MathNet.Numerics.Distributions;

namespace AlliumSativum.Shared.Utils;

public enum DistributionType
{
    Constant,
    Uniform,
    PowerLaw,
    Skewed,
    BiModal,
    Unknown
}

public static class DistributionDetector
{
    public static DistributionType Detect(List<double> values, AttributeEntity attribute)
    {
        var estimation = LogNormal.Estimate(values);

        return DistributionType.Unknown;
    }

}