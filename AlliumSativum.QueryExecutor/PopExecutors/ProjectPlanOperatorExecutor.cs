using System.Diagnostics;
using System.Dynamic;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.Executor;

namespace AlliumSativum.QueryExecutor.PopExecutors;

public sealed class ProjectPlanOperatorExecutor : IPlanOperatorExecutor<ProjectPlanOperator>
{
    public Task<PlanOperator> ExecuteAsync(ProjectPlanOperator pop)
    {
        var stopwatch = Stopwatch.StartNew();

        var childData = pop.Children.Single().ExecutionData.Data;
        var result = new List<Dictionary<string, object>>(childData.Count);
        foreach (var item in childData)
        {
            var projected = new  Dictionary<string, object>(pop.Attributes.Count);
            foreach (var propName in pop.Attributes.Select(x => x.ToDictKey()))
            {
                projected[propName] = item[propName];
            }
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
