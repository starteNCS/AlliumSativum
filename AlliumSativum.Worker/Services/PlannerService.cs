using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Worker.Sdk.Extensions;
using AlliumSativum.Worker.Strategies;
using Grpc.Core;

namespace AlliumSativum.Worker.Services;

public sealed class PlannerService : Planner.PlannerBase
{
    private readonly ILogger<PlannerService> _logger;
    private readonly CatalogDatabase _catalogDatabase;
    private readonly PlannerStrategy _plannerStrategy;

    public PlannerService(
        ILogger<PlannerService> logger,
        CatalogDatabase catalogDatabase,
        PlannerStrategy plannerStrategy)
    {
        _logger = logger;
        _catalogDatabase = catalogDatabase;
        _plannerStrategy = plannerStrategy;
    }
    
    public override async Task<PlanResponse> Plan(GSelectBaseModel request, ServerCallContext context)
    {
        _logger.LogDebug("Begin query planning for: {FromTable}", request.From.TableName);

        var datasources = await _catalogDatabase.QueryAsync<DataSourceEntity>("SELECT Id, Connector FROM Catalog.Datasources WHERE Name = @Name LIMIT 1", new
        {
            Name = request.From.DataSource
        });

        var datasource = datasources.SingleOrDefault();
        if (datasource == null)
        {
            return new PlanResponse()
            {
                Success = false
            };
        }
        
        var planner = _plannerStrategy.GetPlannerOfConnector(datasource.Connector);
        var (proposal, unplanned) = await planner.PlanAsync(datasource.Id, request.FromGrpcModel());

        var response = new PlanResponse
        {
            Success = true,
            Plan = proposal switch
            {
                PushdownSqlPlanOperator psql => new GPlanOperator
                {
                    PushdownSql = new GPushdownSqlPlanOperator
                    {
                        SqlStatement = psql.SqlStatement,
                        DatasourceId = psql.DataSource.ToString()
                    },
                    Cost = proposal.Cost,
                    ExpectedCardinality = proposal.ExpectedCardinality,
                },
                PushdownRestCallPlanOperator prest => new GPlanOperator
                {
                    PushdownRestCall = new GPushdownRestCallPlanOperator
                    {
                        DatasourceId =  prest.DataSource.ToString(),
                        HttpMethod = prest.HttpMethod,
                        Url = prest.Url
                    },
                    Cost = proposal.Cost,
                    ExpectedCardinality = proposal.ExpectedCardinality
                },
                _ => new GPlanOperator()
            },
            Unplanned = unplanned?.ToGrpcModel(),
        };
        
        return response;
    }
}
