namespace AlliumSativum.Shared.Costs.Settings;

public sealed class CostModelSettings
{
    public ProjectCost Project { get; set; }
}

public sealed class ProjectCost
{
    public double BaseCost { get; set; }
    public double PerAttributeCost { get; set; }
}