using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.Executor;

namespace AlliumSativum.QueryExecutor.PopExecutors;

public interface IPlanOperatorExecutor<T> where T : PlanOperator
{
    Task<ExecutorWrapper> ExecuteAsync(T pop, List<object> source);
}
