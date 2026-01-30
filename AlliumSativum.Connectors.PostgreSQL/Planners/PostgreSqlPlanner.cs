using AlliumSativum.Connectors.PostgreSQL.DatabaseConnectors;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Connectors.PostgreSQL.Planners;

public sealed class PostgreSqlPlanner : IPlanner
{
    private readonly CatalogDatabase _catalogDatabase;

    public PostgreSqlPlanner( CatalogDatabase catalogDatabase)
    {
        _catalogDatabase = catalogDatabase;
    }
    
    public async Task<QueryExecutionPlan?> PlanAsync(Guid dataSource, SelectBaseModel selectModel)
    {
        var relation = await _catalogDatabase.GetRelationAsync(dataSource, selectModel.From!.TableName);
        if (relation is null)
        {
            return null;
        }
        
        // TODO: improve here, currently only supporting sequential scan

        var cost = relation.ConnectionOpenMs 
                   + Math.Max(1, relation.Transfer100Ms - relation.ConnectionOpenMs) * (relation.Cardinality / 100);

        return new QueryExecutionPlan
        {
            Cost = cost,
            RootOperator = new PushdownSqlPlanOperator(relation.DataSourceId, selectModel.ToPostgreSqlString())
        };
    }
}
