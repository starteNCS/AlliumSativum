using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.Executor;
using AlliumSativum.Worker.Sdk;

namespace AlliumSativum.QueryExecutor.PopExecutors;

public sealed class PushdownRestPlanOperatorExecutor : IPlanOperatorExecutor<PushdownRestCallPlanOperator>
{
    private readonly IExecutorApi _executorApi;

    public PushdownRestPlanOperatorExecutor(IExecutorApi executorApi)
    {
        _executorApi = executorApi;
    }
    
    public async Task<ExecutorWrapper> ExecuteAsync(PushdownRestCallPlanOperator pop, List<Dictionary<string, object>> source)
    {
        if (source.Count > 0)
        {
            throw new AsSQLExecuteException("Source for PushdownSqlPlanOperatorExecutor should be empty, as pushdown has to be leave of tree.");
        }

        var result = await _executorApi.ExecutePlanAsync(pop);
        if (result == null)
        {
            throw new AsSQLExecuteException("Execution of pushdown SQL plan operator failed.");
        }

        return result;
    }
}
