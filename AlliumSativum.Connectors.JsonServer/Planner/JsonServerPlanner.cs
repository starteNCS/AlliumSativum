using System.Text;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Connectors.JsonServer.Planner;

public sealed class JsonServerPlanner : IPlanner
{
    private readonly CatalogDatabase _catalogDatabase;

    public JsonServerPlanner(CatalogDatabase catalogDatabase)
    {
        _catalogDatabase = catalogDatabase;
    }
    
    public async Task<(List<PlanContainer> proposal, SelectBaseModel? unplanned)> PlanAsync(Guid dataSourceId, SelectBaseModel selectModel)
    {
        var dataSource = await _catalogDatabase.GetDataSourceAsync(dataSourceId);
        if (dataSource is null)
        {
            return ([], null);
        }
        
        var fromRelation = await _catalogDatabase.GetRelationAsync(dataSourceId, selectModel.From!.TableName);
        if (fromRelation is null)
        {
            return ([], null);
        }

        var unplanned = selectModel;
        List<PlanContainer> planOperators = [
            BuildPushDown(dataSource, fromRelation, selectModel.From)
        ];
        foreach (var join in selectModel.Join)
        {
            var joinRelation = await _catalogDatabase.GetRelationAsync(dataSourceId, join.Inner.TableName);
            if (joinRelation is null)
            {
                return ([], null);
            }
            planOperators.Add(BuildPushDown(dataSource, joinRelation, join.Inner));

            var joinAttributes = join.Expression.GetAttributesOfExpression();
            var whereAttributes = unplanned.Where?.GetAttributesOfExpression() ?? [];
            unplanned.AppendHiddenAttribute(joinAttributes);
            unplanned.AppendHiddenAttribute(whereAttributes);
        }
        
        return (planOperators, 
            // technically, the "FROM" (and so the from from the joins) has been proposed. But that is always the case as a pushdown is proposed, we
            // keep it in this model to better map between proposed and unplanned
            unplanned);
    }

    private PlanContainer BuildPushDown(DataSourceEntity dataSource, RelationEntity relation, TableSpecifier from)
    {
        var urlBuilder = new StringBuilder();
        urlBuilder.Append(dataSource.ConnectionString);
        urlBuilder.Append('/');
        urlBuilder.Append(relation.AccessPath);
        
        var cost = relation.ConnectionOpenMs + relation.Transfer100Ms * (relation.Cardinality / 100);

        return new PlanContainer
        {
            Plan = new PushdownRestCallPlanOperator(dataSource.Id, "GET", urlBuilder.ToString(), null)
            {
                Cost = cost,
                ExpectedCardinality = relation.Cardinality,
            },
            PlannedItems = new SelectBaseModel { From = from },
        };
    }
}
