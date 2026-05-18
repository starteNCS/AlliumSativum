using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
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

    
    /// <summary>
    /// Hands the pushdown SQL plan operator to the worker's execution API
    /// </summary>
    /// <param name="pop">The POP to execute</param>
    /// <returns>"pop", containing their results in the data field</returns>
    /// <exception cref="AsSqlExecuteException">Worker ran in some error</exception>
    public async Task<PlanOperator> ExecuteAsync(PushdownSqlPlanOperator pop)
    {
        var result = await _executorApi.ExecutePlanAsync(pop);
        if (result == null) throw new AsSqlExecuteException("Execution of pushdown SQL plan operator failed.");

        _logger.LogDebug("Successfully executed sql pushdown in {ResultMs}ms (Pushdown content: {Content}",
            result.FactualCost, pop.SqlStatement);

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