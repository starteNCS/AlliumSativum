namespace AlliumSativum.Shared.Models.ExecutionPlan;

public sealed class QueryExecutionPlan
{
    public double Cost { get; set; }
    public PlanOperator RootOperator { get; set; }
}
