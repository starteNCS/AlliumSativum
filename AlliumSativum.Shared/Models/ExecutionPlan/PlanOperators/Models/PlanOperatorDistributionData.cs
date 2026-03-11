using AlliumSativum.Shared.Utils;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;

public sealed class PlanOperatorDistributionData
{
    public DistributionType DistributionType { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Mean { get; set; }
    public List<Peak> Peaks { get; set; } = [];

    public PlanOperatorDistributionData CrossJoin(PlanOperatorDistributionData other)
    {
        return new PlanOperatorDistributionData
        {
            DistributionType = DistributionType.Unknown,
            Min = Math.Min(Min, other.Min),
            Max = Math.Max(Max, other.Max),
            Mean = (Mean + other.Mean) / 2,
            Peaks = Peaks.Concat(other.Peaks).ToList()
        };
    }
    
    public class Peak
    {
        public double Position { get; set; }
        public double Height { get; set; }
        public double Mean { get; set; }
        public double StandardDeviation { get; set; }
    }
}
