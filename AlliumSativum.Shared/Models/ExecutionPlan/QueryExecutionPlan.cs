namespace AlliumSativum.Shared.Models.ExecutionPlan;

public sealed class QueryExecutionPlan
{
    public required double Cost { get; set; }
    public required PlanOperator RootOperator { get; set; }
}
