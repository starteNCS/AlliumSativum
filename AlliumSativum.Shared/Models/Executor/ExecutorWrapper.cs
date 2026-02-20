using AlliumSativum.Shared.Models.ExecutionPlan;

namespace AlliumSativum.Shared.Models.Executor;

public sealed class ExecutorWrapper
{
    public PlanOperator PlanOperator { get; set; }
    public List<object> Result { get; set; }
    
    public long FactualCardinality { get; set; }
    public double FactualCost { get; set; }
}
