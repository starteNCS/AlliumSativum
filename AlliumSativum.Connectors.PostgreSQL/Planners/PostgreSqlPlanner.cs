using AlliumSativum.Connectors.PostgreSQL.DatabaseConnectors;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Connectors.PostgreSQL.Planners;

public sealed class PostgreSqlPlanner : IPlanner
{
    private readonly DatasourceDatabase _datasource;

    public PostgreSqlPlanner(DatasourceDatabase datasource)
    {
        _datasource = datasource;
    }
    
    public async Task<List<object>> PlanAsync(SelectBaseModel selectModel)
    {
        return [];
    }
}
