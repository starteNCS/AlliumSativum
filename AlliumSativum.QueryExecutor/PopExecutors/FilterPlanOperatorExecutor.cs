using System.Diagnostics;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;

namespace AlliumSativum.QueryExecutor.PopExecutors;

public sealed class FilterPlanOperatorExecutor : IPlanOperatorExecutor<FilterPlanOperator>
{
    /// <summary>
    ///     Filters the input data by the specified expression
    /// </summary>
    /// <param name="pop">The POP to execute</param>
    /// <returns>"pop", containing their results in the data field</returns>
    public Task<PlanOperator> ExecuteAsync(FilterPlanOperator pop)
    {
        var stopwatch = Stopwatch.StartNew();

        List<Dictionary<string, object>> result = [];
        result.AddRange(pop.Children
            .Single()
            .ExecutionData.Data
            .Where(item => pop.Expression.EvaluatePredicate(item)));

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