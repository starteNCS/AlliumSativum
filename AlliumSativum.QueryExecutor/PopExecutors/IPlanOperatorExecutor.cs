using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.Executor;

namespace AlliumSativum.QueryExecutor.PopExecutors;

public interface IPlanOperatorExecutor<T> where T : PlanOperator
{
    Task<PlanOperator> ExecuteAsync(T pop);
}
