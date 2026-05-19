using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Worker.Sdk.Extensions;
using AlliumSativum.Worker.Strategies;
using Grpc.Core;

namespace AlliumSativum.Worker.Services;

/// <summary>
/// gRPC endpoint for planning
/// </summary>
public sealed class PlannerService : Planner.PlannerBase
{
    private readonly CatalogDatabase _catalogDatabase;
    private readonly ILogger<PlannerService> _logger;
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

    public override async Task<GPlanResponse> Plan(GSelectBaseModel request, ServerCallContext context)
    {
        _logger.LogDebug("Begin query planning for: {FromTable}", request.From.TableName);

        var datasources = await _catalogDatabase.QueryAsync<DataSourceEntity>(
            "SELECT Id, Connector FROM Catalog.Datasources WHERE Name = @Name LIMIT 1", new
            {
                Name = request.From.DataSource
            });

        var datasource = datasources.SingleOrDefault();
        if (datasource == null)
            return new GPlanResponse
            {
                Success = false
            };

        var planner = _plannerStrategy.GetPlannerOfConnector(datasource.Connector);
        var (proposals, unplanned) = await planner.PlanAsync(datasource.Id, request.FromGrpcModel());

        var response = new GPlanResponse
        {
            Success = true,
            Unplanned = unplanned?.ToGrpcModel()
        };

        foreach (var proposal in proposals)
            response.Plans.Add(new GPlanContainer
            {
                Plan = proposal.Plan.ToGrpcModel(),
                Planned = proposal.PlannedItems.ToGrpcModel()
            });

        return response;
    }
}