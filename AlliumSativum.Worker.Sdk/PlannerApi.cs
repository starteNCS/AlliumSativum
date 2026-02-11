using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Worker.Sdk.Extensions;

namespace AlliumSativum.Worker.Sdk;

public interface IPlannerApi
{
    Task<(PlanOperator? proposal, SelectBaseModel? unplanned)> PlanQueryAsync(SelectBaseModel model);
}

public class PlannerApi : IPlannerApi
{
    private readonly Planner.PlannerClient _client;

    public PlannerApi(Planner.PlannerClient client)
    {
        _client = client;
    }

    public async Task<(PlanOperator? proposal, SelectBaseModel? unplanned)> PlanQueryAsync(SelectBaseModel model)
    {
        var response = await _client.PlanAsync(model.ToGrpcModel());
        if (response == null)
        {
            return (null, null);
        }

        return (response.Plan.OperatorTypeCase switch
        {
            GPlanOperator.OperatorTypeOneofCase.PushdownSql => new PushdownSqlPlanOperator(
                Guid.Parse(response.Plan.PushdownSql.DatasourceId),
                response.Plan.PushdownSql.SqlStatement)
            {
                Cost = response.Plan.Cost,
                ExpectedCardinality = response.Plan.ExpectedCardinality,
            },
            GPlanOperator.OperatorTypeOneofCase.PushdownRestCall => new PushdownRestCallPlanOperator(
                Guid.Parse(response.Plan.PushdownRestCall.DatasourceId),
                response.Plan.PushdownRestCall.HttpMethod,
                response.Plan.PushdownRestCall.Url,
                null)
            {
                Cost = response.Plan.Cost,
                ExpectedCardinality = response.Plan.ExpectedCardinality,
            },
            _ => throw new ArgumentException("Expected some plan operator")
        }, response.Unplanned?.FromGrpcModel());
    }
}
