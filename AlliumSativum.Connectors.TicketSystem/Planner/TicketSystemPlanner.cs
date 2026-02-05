using System.Text;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Connectors.TicketSystem.Planner;

public sealed class TicketSystemPlanner : IPlanner
{
    private readonly CatalogDatabase _catalogDatabase;

    public TicketSystemPlanner(CatalogDatabase catalogDatabase)
    {
        _catalogDatabase = catalogDatabase;
    }
    
    public async Task<(PlanOperator? proposal, SelectBaseModel? unplanned)> PlanAsync(Guid dataSourceId, SelectBaseModel selectModel)
    {
        var dataSource = await _catalogDatabase.GetDataSourceAsync(dataSourceId);
        if (dataSource is null)
        {
            return (null, null);
        }
        
        var relation = await _catalogDatabase.GetRelationAsync(dataSourceId, selectModel.From!.TableName);
        if (relation is null)
        {
            return (null, null);
        }
        
        var cost = relation.ConnectionOpenMs + relation.Transfer100Ms * (relation.Cardinality / 100);

        var urlBuilder = new StringBuilder();
        urlBuilder.Append(dataSource.ConnectionString);
        urlBuilder.Append('/');
        urlBuilder.Append(relation.AccessPath);
        
        
        return (new PushdownRestCallPlanOperator(dataSource.Id, "GET", urlBuilder.ToString(), null)
        {
            Cost = cost
        }, selectModel);
    }
}
