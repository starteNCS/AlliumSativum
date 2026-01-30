using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Worker.Sdk.Extensions;

namespace AlliumSativum.Worker.Sdk;

public sealed class PlannerApi
{
    private readonly Planner.PlannerClient _client;

    public PlannerApi(Planner.PlannerClient client)
    {
        _client = client;
    }

    public async Task<PlanOperator?> PlanQueryAsync(SelectBaseModel model)
    {
        var response = await _client.PlanAsync(model.ToGrpcModel());
        if (response == null)
        {
            return null;
        }

        return response.Plan.OperatorTypeCase switch
        {
            GPlanOperator.OperatorTypeOneofCase.PushdownSql => new PushdownSqlPlanOperator(
                Guid.Parse(response.Plan.PushdownSql.DatasourceId),
                response.Plan.PushdownSql.SqlStatement)
            {
                Cost = response.Plan.Cost
            },
            _ => throw new ArgumentException("Expected some plan operator")
        };
    }
}
