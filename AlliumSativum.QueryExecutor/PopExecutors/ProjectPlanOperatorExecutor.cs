using System.Diagnostics;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;

namespace AlliumSativum.QueryExecutor.PopExecutors;

public sealed class ProjectPlanOperatorExecutor : IPlanOperatorExecutor<ProjectPlanOperator>
{
    
    /// <summary>
    /// Projects the specified attributes from the child operator's results
    /// </summary>
    /// <param name="pop">The POP to execute</param>
    /// <returns>"pop", containing their results in the data field</returns>
    public Task<PlanOperator> ExecuteAsync(ProjectPlanOperator pop)
    {
        var stopwatch = Stopwatch.StartNew();

        var childData = pop.Children.Single().ExecutionData.Data;
        var result = new List<Dictionary<string, object>>(childData.Count);
        foreach (var item in childData)
        {
            var projected = new Dictionary<string, object>(pop.Attributes.Count);
            foreach (var propName in pop.Attributes.Select(x => x.ToDictKey())) projected[propName] = item[propName];
            result.Add(projected);
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