using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Worker.Sdk.Extensions;

namespace AlliumSativum.Worker.Sdk;

public interface IPlannerApi
{
    Task<(List<PlanContainer> proposal, SelectDto? unplanned)> PlanQueryAsync(SelectDto model);
}

public class PlannerApi : IPlannerApi
{
    private readonly Planner.PlannerClient _client;

    public PlannerApi(Planner.PlannerClient client)
    {
        _client = client;
    }

    public async Task<(List<PlanContainer> proposal, SelectDto? unplanned)> PlanQueryAsync(SelectDto model)
    {
        var response = await _client.PlanAsync(model.ToGrpcModel());
        if (response == null) return ([], null);

        return (
            response.Plans.Select(plan => new PlanContainer
            {
                Plan = plan.Plan.FromGrpcModel(),
                PlannedItems = plan.Planned.FromGrpcModel()
            }).ToList()
            , response.Unplanned?.FromGrpcModel());
    }
}