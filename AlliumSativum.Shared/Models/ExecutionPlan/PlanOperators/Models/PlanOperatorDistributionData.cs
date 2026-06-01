namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;

public sealed class PlanOperatorDistributionData
{
    public double Min { get; set; }
    public double Max { get; set; }
    public double Mean { get; set; }
    public double MeanBinHeight { get; set; }
    public List<Peak> Peaks { get; set; } = [];

    public class Peak
    {
        public double Position { get; set; }
        public double Height { get; set; }
        public double Mean { get; set; }
        public double StandardDeviation { get; set; }
    }
}