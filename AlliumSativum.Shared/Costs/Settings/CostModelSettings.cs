namespace AlliumSativum.Shared.Costs.Settings;

public sealed class CostModelSettings
{
    public ProjectCost Project { get; set; }
    public FilterCost Filter { get; set; }
    public JoinCost Join { get; set; }
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

public sealed class JoinCost
{
    public NestedLoopJoinCost NestedLoop { get; set; }
    public HashJoinCost Hash { get; set; }
    
    public sealed class NestedLoopJoinCost
    {
        public double BaseCost { get; set; }
    }

    public sealed class HashJoinCost
    {
        public double BaseCost { get; set; }
        public double PerAttributeHashTableInitiation { get; set; }
        public double PerAttributeHashTableLookup { get; set; }
    }
} 