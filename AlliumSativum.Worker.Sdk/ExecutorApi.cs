using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.Executor;
using AlliumSativum.Worker.Sdk.Extensions;

namespace AlliumSativum.Worker.Sdk;

/// <summary>
/// Wrapper for calling the grpc executor endpoints
/// </summary>
public interface IExecutorApi
{
    /// <summary>
    /// Executes the pop on the worker and returns the result of the execution
    /// </summary>
    /// <param name="plan">The pushdown pop</param>
    /// <returns>The result</returns>
    Task<ExecutorWrapper?> ExecutePlanAsync(PlanOperator plan);
}

public sealed class ExecutorApi : IExecutorApi
{
    private readonly Executor.ExecutorClient _executorClient;

    public ExecutorApi(Executor.ExecutorClient executorClient)
    {
        _executorClient = executorClient;
    }

    /// <inheritdoc/>
    public async Task<ExecutorWrapper?> ExecutePlanAsync(PlanOperator plan)
    {
        var response = await _executorClient.ExecuteAsync(plan.ToGrpcModel());
        return response.FromGrpcModel();
    }
}