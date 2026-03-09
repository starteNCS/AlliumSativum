using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.Executor;
using AlliumSativum.Worker.Sdk;
using Microsoft.Extensions.Logging;

namespace AlliumSativum.QueryExecutor.PopExecutors;

public sealed class PushdownSqlPlanOperatorExecutor : IPlanOperatorExecutor<PushdownSqlPlanOperator>
{
    private readonly IExecutorApi _executorApi;
    private readonly ILogger<PushdownSqlPlanOperatorExecutor> _logger;

    public PushdownSqlPlanOperatorExecutor(
        IExecutorApi executorApi,
        ILogger<PushdownSqlPlanOperatorExecutor> logger)
    {
        _executorApi = executorApi;
        _logger = logger;
    }
    
    public async Task<PlanOperator> ExecuteAsync(PushdownSqlPlanOperator pop)
    {
        var result = await _executorApi.ExecutePlanAsync(pop);
        if (result == null)
        {
            throw new AsSQLExecuteException("Execution of pushdown SQL plan operator failed.");
        }
        
        _logger.LogDebug("Successfully executed sql pushdown in {resultMs}ms (Pushdown content: {Content}", result.FactualCost, pop.SqlStatement);
        
        var executionData = new PlanOperatorExecutionData
        {
            Materialized = true,
            ActualCardinality = result.FactualCardinality,
            ActualCost = result.FactualCost, // TODO: get actual cost from worker
            Data = result.Result
        };
        pop.ExecutionData = executionData;

        return pop;
    }
}
