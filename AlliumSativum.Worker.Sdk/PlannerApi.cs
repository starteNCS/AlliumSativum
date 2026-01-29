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

    public async Task<List<QueryExecutionPlan>> PlanQueryAsync(SelectBaseModel model)
    {
        var plans = await _client.PlanAsync(model.ToGrpcModel());
        if (plans == null)
        {
            return [];
        }
        
        return plans.Plans.Select(p => new QueryExecutionPlan
        {
            Cost = p.Cost,
            RootOperator = p.RootOperator.OperatorTypeCase switch
            {
                GPlanOperator.OperatorTypeOneofCase.PushdownSql => new PushdownSqlPlanOperator
                {
                    SqlStatement = p.RootOperator.PushdownSql.SqlStatement
                },
                GPlanOperator.OperatorTypeOneofCase.None => throw new ArgumentException("Expected some plan operator")
            }
        }).ToList();
    }
}
