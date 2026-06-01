using AlliumSativum.Shared.Models.ExecutionPlan;

namespace AlliumSativum.QueryExecutor.PopExecutors;

public interface IPlanOperatorExecutor<T> where T : PlanOperator
{
    /// <summary>
    ///     Executes the given plan operator, and returns it with its execution data field populated.
    ///     The returned plan operator is the same instance as the input, or a different one, but must have the same execution
    ///     data field.
    /// </summary>
    /// <param name="pop">The pop to execute</param>
    /// <returns>A equal pop containing results</returns>
    Task<PlanOperator> ExecuteAsync(T pop);
}