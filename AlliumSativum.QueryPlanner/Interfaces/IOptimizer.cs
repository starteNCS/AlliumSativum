using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Interfaces;

public interface IOptimizer
{
    /// <summary>
    ///     Optimizes the given SelectBaseModel into a QueryExecutionPlan
    ///     Operates in multiple steps:
    ///     (✅ implementation, ☑️ test)
    ///     - create on-premise only join tree ✅ ☑️
    ///     - split the given model into TABLES ✅ ☑️
    ///     - check which WHERE expressions can be 100% assigned to one table ✅ ☑️
    ///     - append hidden selects ✅ ☑️
    ///     - check joins, merge multiple tables into one sub plan if possible ✅ ☑️
    ///     - check WHERE again, if any more can be pushed down ✅
    ///     - propose to the worker ✅
    ///     - check what it did not accept and add POP's to the plan accordingly
    ///     - Join Order Optimization of on-premise joins
    ///     - rule/cost-based check what POP's can be accumulated for cost reduction (if any)
    ///     - accumulate cost
    ///     - return plan with cost
    ///     -
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="AsSqlOptimizeException"></exception>
    Task<List<QueryExecutionPlan>> OptimizeAsync(SelectDto model, bool prune = true);
}