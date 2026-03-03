using System.Diagnostics;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

namespace AlliumSativum.QueryExecutor.PopExecutors;

public sealed class FilterPlanOperatorExecutor : IPlanOperatorExecutor<FilterPlanOperator>
{
    public Task<PlanOperator> ExecuteAsync(FilterPlanOperator pop)
    {
        var stopwatch = Stopwatch.StartNew();

        List<Dictionary<string, object>> result = [];
        foreach (var item in pop.Children.Single().ExecutionData.Data)
        {
            if (pop.Expression.EvaluatePredicate(item))
            {
                result.Add(item);
            }
        }
        
        stopwatch.Stop();
        var executionData = new PlanOperatorExecutionData
        {
            Materialized = true,
            ActualCardinality = result.Count,
            ActualCost = stopwatch.Elapsed.TotalMilliseconds,
            Data = result
        };
        pop.ExecutionData = executionData;

        return Task.FromResult<PlanOperator>(pop);
    }
}
