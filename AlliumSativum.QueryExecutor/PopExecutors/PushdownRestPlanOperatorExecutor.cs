using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Worker.Sdk;
using Microsoft.Extensions.Logging;

namespace AlliumSativum.QueryExecutor.PopExecutors;

public sealed class PushdownRestPlanOperatorExecutor : IPlanOperatorExecutor<PushdownRestCallPlanOperator>
{
    private readonly IExecutorApi _executorApi;
    private readonly ILogger<PushdownRestPlanOperatorExecutor> _logger;

    public PushdownRestPlanOperatorExecutor(
        IExecutorApi executorApi,
        ILogger<PushdownRestPlanOperatorExecutor> logger)
    {
        _executorApi = executorApi;
        _logger = logger;
    }

    public async Task<PlanOperator> ExecuteAsync(PushdownRestCallPlanOperator pop)
    {
        var result = await _executorApi.ExecutePlanAsync(pop);
        if (result == null) throw new AsSqlExecuteException("Execution of pushdown SQL plan operator failed.");

        _logger.LogDebug("Successfully executed rest pushdown in {ResultMs}ms (Pushdown content: {Content}",
            result.FactualCost, pop.Url);

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