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

    public async Task<QueryExecutionPlan?> PlanQueryAsync(SelectBaseModel model)
    {
        var plan = await _client.PlanAsync(model.ToGrpcModel());
        if (plan == null)
        {
            return null;
        }

        return new QueryExecutionPlan
        {
            Cost = plan.Plan.Cost,
            RootOperator = plan.Plan.RootOperator.OperatorTypeCase switch
            {
                GPlanOperator.OperatorTypeOneofCase.PushdownSql => new PushdownSqlPlanOperator(
                    Guid.Parse(plan.Plan.RootOperator.PushdownSql.DatasourceId), 
                    plan.Plan.RootOperator.PushdownSql.SqlStatement),
                _ => throw new ArgumentException("Expected some plan operator")
            }
        };
    }
}
