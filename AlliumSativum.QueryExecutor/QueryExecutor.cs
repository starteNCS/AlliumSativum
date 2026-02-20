using AlliumSativum.QueryExecutor.PopExecutors;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

namespace AlliumSativum.QueryExecutor;

public sealed class QueryExecutor
{
    private readonly ProjectPlanOperatorExecutor _projectPlanOperatorExecutor;
    private readonly PushdownSqlPlanOperatorExecutor _pushdownSqlPlanOperatorExecutor;
    private readonly PushdownRestPlanOperatorExecutor _pushdownRestPlanOperatorExecutor;

    public QueryExecutor(
        ProjectPlanOperatorExecutor projectPlanOperatorExecutor,
        PushdownSqlPlanOperatorExecutor pushdownSqlPlanOperatorExecutor,
        PushdownRestPlanOperatorExecutor pushdownRestPlanOperatorExecutor)
    {
        _projectPlanOperatorExecutor = projectPlanOperatorExecutor;
        _pushdownSqlPlanOperatorExecutor = pushdownSqlPlanOperatorExecutor;
        _pushdownRestPlanOperatorExecutor = pushdownRestPlanOperatorExecutor;
    }
    
    public async Task<List<Dictionary<string, object>>> ExecuteAsync(PlanOperator root)
    {
        var stack = new Stack<PlanOperator>();
        stack.Push(root);
        // TODO: add support for parallel branches
        while (root.Children.Count > 0)
        {
            root = root.Children[0];
            stack.Push(root);
        }

        List<Dictionary<string, object>> currentItems = [];
        while (stack.Count > 0)
        {
            var item = stack.Pop();
            
            var result = item switch
            {
                ProjectPlanOperator project => await _projectPlanOperatorExecutor.ExecuteAsync(project, currentItems),
                PushdownSqlPlanOperator pushdown => await _pushdownSqlPlanOperatorExecutor.ExecuteAsync(pushdown, []),
                PushdownRestCallPlanOperator pushdown => await _pushdownRestPlanOperatorExecutor.ExecuteAsync(pushdown, []),
                _ => throw new NotSupportedException($"Unsupported plan operator: {root.GetType().Name}")
            };

            currentItems = result.Result;
        }
        

        return currentItems;
    }
}