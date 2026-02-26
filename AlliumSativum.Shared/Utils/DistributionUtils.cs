using AlliumSativum.Shared.Database.Entities;

namespace AlliumSativum.Shared.Utils;

public static class DistributionUtils
{
    public static DistributionType GetDistributionType(AttributeEntity attributeEntity)
    {
        // Basic heuristic to detect if uniform.
        // expresses the standard deviation as a percentage of the mean
        var coefficientOfVariation = attributeEntity.StandardDeviation / attributeEntity.Mean;
        if (coefficientOfVariation < 0.15)
        {
            return DistributionType.QuasiUniform(coefficientOfVariation);
        }

        // Basic heuristic to detect if quasi-constant
        // distribution is so heavily skewed, that the one outlier is the "quasi constant" field
        if (attributeEntity is { Skewness: > 10, Kurtosis: > 50 } && coefficientOfVariation > 2)
        {
            return DistributionType.QuasiConstant();
        }
        
        // Basic heuristic to detect if multi-modal
        var bimodalityCoefficient = (Math.Pow(attributeEntity.Skewness, 2) + 1) / (attributeEntity.Kurtosis + 3);
        if (bimodalityCoefficient > 0.555) // 5/9
        {
            return DistributionType.MultiModal(bimodalityCoefficient);
        }
        
        // Basic heuristic to detect if power law
        // Highly skewed to one side with a long tail on the other
        if(attributeEntity is { Skewness: > 2, Kurtosis: > 3 })
        {
            return DistributionType.PowerLaw();
        }
        
        // Fallback to skewed distribution
        return DistributionType.Skewed();
    }
}

public abstract class DistributionType
{
    public static QuasiUniformDistributionType QuasiUniform(double coefficientOfVariation) 
        => new QuasiUniformDistributionType(coefficientOfVariation);

    public static PowerLawDistributionType PowerLaw()
        => new PowerLawDistributionType();
    
    public static SkewedDistributionType Skewed()
    => new SkewedDistributionType();

    public static QuasiConstantDistributionType QuasiConstant()
        => new QuasiConstantDistributionType();
    
    public static MultiModalDistributionType MultiModal(double bimodalityCoefficient)
        => new MultiModalDistributionType(bimodalityCoefficient);
}

public sealed class QuasiUniformDistributionType : DistributionType
{
    public double CoefficientOfVariation { get; }

    public QuasiUniformDistributionType(double coefficientOfVariation)
    {
        CoefficientOfVariation = coefficientOfVariation;
    }
}

public sealed class PowerLawDistributionType : DistributionType
{
}

public sealed class SkewedDistributionType : DistributionType
{
}

public sealed class QuasiConstantDistributionType : DistributionType
{
}

public sealed class MultiModalDistributionType : DistributionType
{
    public double BimodalityCoefficient { get; set; }
    
    public MultiModalDistributionType(double bimodalityCoefficient)
    {
        BimodalityCoefficient = bimodalityCoefficient;
    }
}