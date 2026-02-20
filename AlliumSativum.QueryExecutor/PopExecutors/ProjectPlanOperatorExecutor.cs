using System.Diagnostics;
using System.Dynamic;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.Executor;

namespace AlliumSativum.QueryExecutor.PopExecutors;

public sealed class ProjectPlanOperatorExecutor : IPlanOperatorExecutor<ProjectPlanOperator>
{
    public Task<ExecutorWrapper> ExecuteAsync(ProjectPlanOperator pop, List<object> source)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new List<object>(source.Count);
        foreach (var item in source)
        {
            var type = item.GetType();
            var projected = new ExpandoObject() as IDictionary<string, object>;
        
            foreach (var propName in pop.Attributes.Select(x => x.AttributeName))
            {
                var value = type.GetProperty(propName)?.GetValue(item);
                projected[propName] = value;
            }
            result.Add(projected);
        }
        stopwatch.Stop();

        return Task.FromResult(new ExecutorWrapper()
        {
            Result = result,
            FactualCardinality = result.Count,
            FactualCost = stopwatch.ElapsedMilliseconds,
            PlanOperator = pop
        });
    }
}
