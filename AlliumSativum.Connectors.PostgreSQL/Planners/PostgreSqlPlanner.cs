using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Connectors.PostgreSQL.Planners;

public sealed class PostgreSqlPlanner : IPlanner
{
    public async Task<List<object>> PlanAsync(SelectBaseModel selectModel)
    {
        return [];
    }
}
