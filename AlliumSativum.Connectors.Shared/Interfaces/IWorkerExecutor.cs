using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.Executor;

namespace AlliumSativum.Connectors.Shared.Interfaces;

public interface IWorkerExecutor
{
    Task<ExecutorWrapper> ExecuteAsync(PlanOperator @operator);
}