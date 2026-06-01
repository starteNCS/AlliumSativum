using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.Executor;

namespace AlliumSativum.Connectors.Shared.Interfaces;

public interface IWorkerExecutor
{
    /// <summary>
    ///     Pushes down the work as defined by the operator to the worker
    /// </summary>
    /// <param name="operator">POP to execute</param>
    /// <returns>Execution result and metadata</returns>
    Task<ExecutorWrapper> ExecuteAsync(PlanOperator @operator);
}