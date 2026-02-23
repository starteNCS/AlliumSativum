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
    
    public async Task<PlanOperator> ExecuteAsync(PushdownRestCallPlanOperator pop)
    {
        var result = await _executorApi.ExecutePlanAsync(pop);
        if (result == null)
        {
            throw new AsSQLExecuteException("Execution of pushdown SQL plan operator failed.");
        }
        
        var executionData = new PlanOperatorExecutionData
        {
            Materialized = true,
            ActualCardinality = result.FactualCardinality,
            ActualCost = result.FactualCost,
            Data = result.Result
        };
        pop.ExecutionData = executionData;

        return pop;
    }
}
