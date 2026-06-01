using System.Diagnostics;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;

namespace AlliumSativum.QueryExecutor.PopExecutors.Join;

public sealed class NestedLoopJoinPlanOperatorExecutor : IPlanOperatorExecutor<NestedLoopJoinPlanOperator>
{
    /// <summary>
    ///     Joins both child data sets by using a nested loop join, and applying the specified expression as a predicate to the
    ///     merged rows.
    /// </summary>
    /// <param name="pop">The POP to execute</param>
    /// <returns>"pop", containing their results in the data field</returns>
    public Task<PlanOperator> ExecuteAsync(NestedLoopJoinPlanOperator pop)
    {
        var stopwatch = Stopwatch.StartNew();
        var left = pop.Left.ExecutionData.Data;
        var right = pop.Right.ExecutionData.Data;

        var result = new List<Dictionary<string, object>>();
        foreach (var leftRow in left)
        foreach (var rightRow in right)
        {
            var merged = Merge(leftRow, rightRow);
            if (pop.Expression.EvaluatePredicate(merged)) result.Add(merged);
        }

        stopwatch.Stop();

        pop.ExecutionData = new PlanOperatorExecutionData
        {
            Data = result,
            Materialized = true,
            ActualCardinality = result.Count,
            ActualCost = stopwatch.Elapsed.TotalMilliseconds
        };

        return Task.FromResult<PlanOperator>(pop);
    }

    /// <summary>
    ///     Merges two rows into one by concatenating their key-value pairs
    /// </summary>
    /// <remarks>In case of key collisions, the value from the right row will be used.</remarks>
    /// <param name="left">The first row</param>
    /// <param name="right">The second row</param>
    /// <returns>The merge result</returns>
    private static Dictionary<string, object> Merge(Dictionary<string, object> left, Dictionary<string, object> right)
    {
        var merged = new Dictionary<string, object>(left);
        foreach (var kvp in right) merged[kvp.Key] = kvp.Value;

        return merged;
    }
}