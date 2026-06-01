using System.Text;
using AlliumSativum.Connectors.Shared.CatalogUtils;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Connectors.JsonServer.Planner;

public sealed class JsonServerPlanner : IPlanner
{
    private readonly CatalogDatabase _catalogDatabase;
    private readonly CatalogDistributionUtils _distributionUtils;

    public JsonServerPlanner(
        CatalogDatabase catalogDatabase,
        CatalogDistributionUtils distributionUtils)
    {
        _catalogDatabase = catalogDatabase;
        _distributionUtils = distributionUtils;
    }

    /// <inheritdoc />
    public async Task<(List<PlanContainer> proposal, SelectDto? unplanned)> PlanAsync(Guid dataSourceId,
        SelectDto selectModel)
    {
        var dataSource = await _catalogDatabase.GetDataSourceAsync(dataSourceId);
        if (dataSource is null) return ([], null);

        var fromRelation = await _catalogDatabase.GetRelationAsync(dataSourceId, selectModel.From!.TableName);
        if (fromRelation is null) return ([], null);

        var fromRelationAttribtues = await _catalogDatabase.GetAttributesOfRelationAsync(fromRelation.Id);

        var distribution = await _distributionUtils.GetAttributeDistributionsAsync(selectModel.GetAffectedAttributes());

        var unplanned = selectModel;
        List<PlanContainer> planOperators =
        [
            BuildPushDown(
                dataSource,
                fromRelation,
                fromRelationAttribtues,
                selectModel.From,
                distribution
                    .Where(x => x.Key.IsInTable(selectModel.From))
                    .ToDictionary(x => x.Key, x => x.Value))
        ];
        foreach (var join in selectModel.Join)
        {
            var joinRelation = await _catalogDatabase.GetRelationAsync(dataSourceId, join.Inner.TableName);
            if (joinRelation is null) return ([], null);

            var joinRelationAttributes = await _catalogDatabase.GetAttributesOfRelationAsync(joinRelation.Id);


            planOperators.Add(
                BuildPushDown(
                    dataSource,
                    joinRelation,
                    joinRelationAttributes,
                    join.Inner,
                    distribution
                        .Where(x => x.Key.IsInTable(join.Inner))
                        .ToDictionary(x => x.Key, x => x.Value))
            );

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

    private PlanContainer BuildPushDown(DataSourceEntity dataSource, RelationEntity relation,
        List<AttributeEntity> attributes, TableSpecifier from,
        Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData)
    {
        var urlBuilder = new StringBuilder();
        urlBuilder.Append(dataSource.ConnectionString);
        urlBuilder.Append('/');
        urlBuilder.Append(relation.AccessPath);

        // only use T100, as scraping the metrics stores the Tall time (we cannot paginate the json server)
        var cost = relation.ConnectionOpenMs + relation.Transfer100Ms;

        return new PlanContainer
        {
            Plan = new PushdownRestCallPlanOperator(dataSource.Id, "GET", urlBuilder.ToString(), null)
            {
                Cost = cost,
                ExpectedCardinality = relation.Cardinality,
                Self = from,
                DistributionData = distributionData,
                Width = attributes.Count
            },
            PlannedItems = new SelectDto { From = from }
        };
    }
}