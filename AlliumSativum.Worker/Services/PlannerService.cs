using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Database.Entities;
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
    
    public override async Task<Void> Plan(GSelectBaseModel request, ServerCallContext context)
    {
        _logger.LogDebug("Begin query planning for: {FromTable}", request.From.TableName);

        var datasources = await _catalogDatabase.QueryAsync<DataSourceEntity>("SELECT Connector FROM Catalog.Datasources WHERE Name = @Name LIMIT 1", new
        {
            Name = request.From.DataSource
        });

        var datasource = datasources.SingleOrDefault();
        if (datasource == null)
        {
            return new Void();
        }
        
        var planner = _plannerStrategy.GetPlannerOfConnector(datasource.Connector);
        await planner.PlanAsync(request.FromGrpcModel());

        return new Void();
    }
}
