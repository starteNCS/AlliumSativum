using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Worker.Sdk.Extensions;
using static AlliumSativum.Worker.GPlanOperator.OperatorTypeOneofCase;

namespace AlliumSativum.Worker.Sdk;

public interface IPlannerApi
{
    Task<(List<PlanContainer> proposal, SelectBaseModel? unplanned)> PlanQueryAsync(SelectBaseModel model);
}

public class PlannerApi : IPlannerApi
{
    private readonly Planner.PlannerClient _client;

    public PlannerApi(Planner.PlannerClient client)
    {
        _client = client;
    }

    public async Task<(List<PlanContainer> proposal, SelectBaseModel? unplanned)> PlanQueryAsync(SelectBaseModel model)
    {
        var response = await _client.PlanAsync(model.ToGrpcModel());
        if (response == null)
        {
            return ([], null);
        }

        return (
            response.Plans.Select(plan => new PlanContainer
            {
                Plan = plan.Plan.OperatorTypeCase switch
                {
                    PushdownSql => new PushdownSqlPlanOperator(
                        Guid.Parse(plan.Plan.PushdownSql.DatasourceId),
                        plan.Plan.PushdownSql.SqlStatement)
                    {
                        Cost = plan.Plan.Cost,
                        ExpectedCardinality = plan.Plan.ExpectedCardinality,
                    },
                    PushdownRestCall => new PushdownRestCallPlanOperator(
                        Guid.Parse(plan.Plan.PushdownRestCall.DatasourceId),
                        plan.Plan.PushdownRestCall.HttpMethod,
                        plan.Plan.PushdownRestCall.Url,
                        null)
                    {
                        Cost = plan.Plan.Cost,
                        ExpectedCardinality = plan.Plan.ExpectedCardinality,
                    },
                    _ => throw new ArgumentException("Expected some plan operator"),
                },
                PlannedItems = plan.Planned.FromGrpcModel()
            }).ToList()
            , response.Unplanned?.FromGrpcModel());
    }
}
