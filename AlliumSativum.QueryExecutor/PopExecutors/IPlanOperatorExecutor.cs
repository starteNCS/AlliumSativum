using AlliumSativum.Shared.Models.ExecutionPlan;

namespace AlliumSativum.QueryExecutor.PopExecutors;

public interface IPlanOperatorExecutor<T> where T : PlanOperator
{
    Task<PlanOperator> ExecuteAsync(T pop);
}