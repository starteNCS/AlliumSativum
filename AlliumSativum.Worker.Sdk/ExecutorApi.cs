using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.Executor;
using AlliumSativum.Worker.Sdk.Extensions;

namespace AlliumSativum.Worker.Sdk;

public interface IExecutorApi
{
    Task<ExecutorWrapper?> ExecutePlanAsync(PlanOperator plan);
}

public sealed class ExecutorApi : IExecutorApi
{
    private readonly Executor.ExecutorClient _executorClient;

    public ExecutorApi(Executor.ExecutorClient executorClient)
    {
        _executorClient = executorClient;
    }
    
    public async Task<ExecutorWrapper?> ExecutePlanAsync(PlanOperator plan)
    {
        var response = await _executorClient.ExecuteAsync(plan.ToGrpcModel());
        return response.FromGrpcModel();
    }
}
