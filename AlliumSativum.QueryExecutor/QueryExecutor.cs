using AlliumSativum.QueryExecutor.PopExecutors;
using AlliumSativum.QueryExecutor.PopExecutors.Join;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;

namespace AlliumSativum.QueryExecutor;

public sealed class QueryExecutor
{
    private readonly FilterPlanOperatorExecutor _filterPlanOperatorExecutor;
    private readonly HashJoinPlanOperatorExecutor _hashJoinPlanOperatorExecutor;
    private readonly NestedLoopJoinPlanOperatorExecutor _nestedLoopJoinPlanOperatorExecutor;
    private readonly ProjectPlanOperatorExecutor _projectPlanOperatorExecutor;
    private readonly PushdownRestPlanOperatorExecutor _pushdownRestPlanOperatorExecutor;
    private readonly PushdownSqlPlanOperatorExecutor _pushdownSqlPlanOperatorExecutor;

    public QueryExecutor(
        ProjectPlanOperatorExecutor projectPlanOperatorExecutor,
        FilterPlanOperatorExecutor filterPlanOperatorExecutor,
        PushdownSqlPlanOperatorExecutor pushdownSqlPlanOperatorExecutor,
        PushdownRestPlanOperatorExecutor pushdownRestPlanOperatorExecutor,
        NestedLoopJoinPlanOperatorExecutor nestedLoopJoinPlanOperatorExecutor,
        HashJoinPlanOperatorExecutor hashJoinPlanOperatorExecutor)
    {
        _projectPlanOperatorExecutor = projectPlanOperatorExecutor;
        _filterPlanOperatorExecutor = filterPlanOperatorExecutor;
        _pushdownSqlPlanOperatorExecutor = pushdownSqlPlanOperatorExecutor;
        _pushdownRestPlanOperatorExecutor = pushdownRestPlanOperatorExecutor;
        _nestedLoopJoinPlanOperatorExecutor = nestedLoopJoinPlanOperatorExecutor;
        _hashJoinPlanOperatorExecutor = hashJoinPlanOperatorExecutor;
    }

    public async Task<List<Dictionary<string, object>>> ExecuteAsync(ParallelQueryExecutionPlan root)
    {
        if (root.AwaitableStacks.Count > 0)
            // hand off to workers, especially those close to the data source
            await Task.WhenAll(root.AwaitableStacks.Select(ExecuteAsync));

        PlanOperator? latestPop = null;
        while (root.Continuation.Count > 0)
        {
            latestPop = root.Continuation.Pop();

            latestPop = await (latestPop switch
            {
                ProjectPlanOperator project => _projectPlanOperatorExecutor.ExecuteAsync(project),
                FilterPlanOperator filter => _filterPlanOperatorExecutor.ExecuteAsync(filter),
                PushdownSqlPlanOperator pushdown => _pushdownSqlPlanOperatorExecutor.ExecuteAsync(pushdown),
                PushdownRestCallPlanOperator pushdown => _pushdownRestPlanOperatorExecutor.ExecuteAsync(pushdown),
                NestedLoopJoinPlanOperator join => _nestedLoopJoinPlanOperatorExecutor.ExecuteAsync(join),
                HashJoinPlanOperator join => _hashJoinPlanOperatorExecutor.ExecuteAsync(join),
                _ => throw new NotSupportedException($"Unsupported plan operator: {latestPop.GetType().Name}")
            });
        }

        return latestPop?.ExecutionData.Data ?? [];
    }

    public Task<List<Dictionary<string, object>>> ExecuteAsync(PlanOperator root)
    {
        var parallelStacks = ToParallelStacks(root);
        return ExecuteAsync(parallelStacks);
    }

    public static ParallelQueryExecutionPlan ToParallelStacks(PlanOperator root)
    {
        var continuation = new List<PlanOperator>();
        var branches = new List<ParallelQueryExecutionPlan>();

        var current = root;
        while (current != null)
        {
            if (current.Children.Count > 1)
            {
                branches = current.Children.Select(ToParallelStacks).ToList();
                continuation.Add(current);
                break;
            }

            continuation.Add(current);
            current = current.Children.FirstOrDefault();
        }

        return new ParallelQueryExecutionPlan
        {
            AwaitableStacks = branches,
            Continuation = new Stack<PlanOperator>(continuation)
        };
    }
}