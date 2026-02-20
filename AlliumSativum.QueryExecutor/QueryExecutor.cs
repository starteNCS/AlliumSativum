using AlliumSativum.QueryExecutor.PopExecutors;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

namespace AlliumSativum.QueryExecutor;

public sealed class QueryExecutor
{
    private readonly ProjectPlanOperatorExecutor _projectPlanOperatorExecutor;
    private readonly PushdownSqlPlanOperatorExecutor _pushdownSqlPlanOperatorExecutor;

    public QueryExecutor(
        ProjectPlanOperatorExecutor projectPlanOperatorExecutor,
        PushdownSqlPlanOperatorExecutor pushdownSqlPlanOperatorExecutor)
    {
        _projectPlanOperatorExecutor = projectPlanOperatorExecutor;
        _pushdownSqlPlanOperatorExecutor = pushdownSqlPlanOperatorExecutor;
    }
    
    public async Task<List<object>> ExecuteAsync(PlanOperator root)
    {
        var result = root switch
        {
            ProjectPlanOperator project => await _projectPlanOperatorExecutor.ExecuteAsync(project, []),
            PushdownSqlPlanOperator pushdown => await _pushdownSqlPlanOperatorExecutor.ExecuteAsync(pushdown, []),
            _ => throw new NotSupportedException($"Unsupported plan operator: {root.GetType().Name}")
        };

        return result.Result;
    }
}