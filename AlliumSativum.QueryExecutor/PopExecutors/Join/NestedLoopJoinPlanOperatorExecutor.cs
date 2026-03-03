using System.Diagnostics;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.QueryExecutor.PopExecutors.Join;

public sealed class NestedLoopJoinPlanOperatorExecutor : IPlanOperatorExecutor<NestedLoopJoinPlanOperator>
{
    public Task<PlanOperator> ExecuteAsync(NestedLoopJoinPlanOperator pop)
    {
        var stopwatch = Stopwatch.StartNew();
        var left = pop.Left.ExecutionData.Data;
        var right = pop.Right.ExecutionData.Data;

        var result = new List<Dictionary<string, object>>();
        foreach (var leftRow in left)
        {
            foreach (var rightRow in right)
            {
                var merged = Merge(leftRow, rightRow);
                if (pop.Expression.EvaluatePredicate(merged))
                {
                    result.Add(merged);
                }
            }
        }

        stopwatch.Stop();

        pop.ExecutionData = new PlanOperatorExecutionData
        {
            Data = result,
            Materialized = true,
            ActualCardinality = result.Count,
            ActualCost = stopwatch.Elapsed.TotalMilliseconds,
        };

        return Task.FromResult<PlanOperator>(pop);
    }

    private static Dictionary<string, object> Merge(Dictionary<string, object> left, Dictionary<string, object> right)
    {
        var merged = new Dictionary<string, object>(left);
        foreach (var kvp in right)
        {
            merged[kvp.Key] = kvp.Value;
        }
        
        return merged;
    }
}
