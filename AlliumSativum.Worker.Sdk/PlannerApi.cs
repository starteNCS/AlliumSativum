using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Worker.Sdk.Extensions;

namespace AlliumSativum.Worker.Sdk;

/// <summary>
/// Wrapper for calling the grpc planner endpoints
/// </summary>
public interface IPlannerApi
{
    /// <summary>
    /// Plan a pushdown pop for the given sDTO
    /// </summary>
    /// <param name="model">Input sDTO</param>
    /// <returns>
    /// proposal: One or more pushdown proposals, depending on source capabilities
    /// unplanned: Everything that was not planned, i.e. everything that needs to be executed on premises
    /// </returns>
    Task<(List<PlanContainer> proposal, SelectDto? unplanned)> PlanQueryAsync(SelectDto model);
}

public class PlannerApi : IPlannerApi
{
    private readonly Planner.PlannerClient _client;

    public PlannerApi(Planner.PlannerClient client)
    {
        _client = client;
    }

    /// <inheritdoc/>
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