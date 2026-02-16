using AlliumSativum.Connectors.PostgreSQL.DatabaseConnectors;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Connectors.PostgreSQL.Planners;

public sealed class PostgreSqlPlanner : IPlanner
{
    private readonly CatalogDatabase _catalogDatabase;

    public PostgreSqlPlanner(CatalogDatabase catalogDatabase)
    {
        _catalogDatabase = catalogDatabase;
    }
    
    public async Task<(List<PlanContainer> proposal, SelectBaseModel? unplanned)> PlanAsync(Guid dataSourceId, SelectBaseModel selectModel)
    {
        var relation = await _catalogDatabase.GetRelationAsync(dataSourceId, selectModel.From!.TableName);
        if (relation is null)
        {
            return ([], null);
        }
        
        // TODO: adjust cost for filters and joins
        var cost = relation.ConnectionOpenMs + relation.Transfer100Ms * (relation.Cardinality / 100);
        
        // TODO: Calculate adjusted cardinality
        var cardinality = relation.Cardinality;

        return ([
            new PlanContainer
            {
                Plan = new PushdownSqlPlanOperator(relation.DataSourceId, selectModel.ToPostgreSqlString())
                {
                    Cost = cost,
                    ExpectedCardinality = cardinality,
                },
                PlannedItems = selectModel
            }
        ], null);
    }
}
