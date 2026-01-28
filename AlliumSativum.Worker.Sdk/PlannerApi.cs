using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Worker.Sdk.Extensions;

namespace AlliumSativum.Worker.Sdk;

public sealed class PlannerApi
{
    private readonly Planner.PlannerClient _client;

    public PlannerApi(Planner.PlannerClient client)
    {
        _client = client;
    }

    public async Task<object> PlanQueryAsync(SelectBaseModel model)
    {
        
        await _client.PlanAsync(model.ToGrpcModel());
        return null!;
    }
}
