namespace AlliumSativum.Shared.Costs.Settings;

public sealed class CostModelSettings
{
    public ProjectCost Project { get; set; }
    public FilterCost Filter { get; set; }
    public SelectivityEstimationSettings SelectivityEstimation { get; set; }
}

public sealed class ProjectCost
{
    public double BaseCost { get; set; }
    public double PerAttributeCost { get; set; }
}

public sealed class FilterCost
{
    public double BaseCost { get; set; }
    public double PerAttributeCostNumeric { get; set; }
    public double PerAttributeCostString { get; set; }
}

public sealed class SelectivityEstimationSettings
{
    public double PenaltyForConstant { get; set; }
}