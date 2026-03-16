namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;

public sealed class PlanOperatorExecutionData
{
    public bool Materialized { get; set; } = false;

    public long ActualCardinality { get; set; }

    public double ActualCost { get; set; }

    public List<Dictionary<string, object>> Data { get; set; } = [];
}