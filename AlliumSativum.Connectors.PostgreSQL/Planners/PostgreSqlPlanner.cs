using AlliumSativum.Connectors.PostgreSQL.DatabaseConnectors;
using AlliumSativum.Connectors.Shared.CatalogUtils;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Connectors.PostgreSQL.Planners;

public sealed class PostgreSqlPlanner : IPlanner
{
    private readonly CatalogDatabase _catalogDatabase;
    private readonly DatasourceDatabase _datasource;
    private readonly CatalogDistributionUtils _distributionUtils;

    public PostgreSqlPlanner(
        CatalogDatabase catalogDatabase,
        CatalogDistributionUtils distributionUtils,
        DatasourceDatabase datasource)
    {
        _catalogDatabase = catalogDatabase;
        _distributionUtils = distributionUtils;
        _datasource = datasource;
    }

    /// <inheritdoc />
    public async Task<(List<PlanContainer> proposal, SelectDto? unplanned)> PlanAsync(Guid dataSourceId,
        SelectDto selectModel)
    {
        var relation = await _catalogDatabase.GetRelationAsync(dataSourceId, selectModel.From!.TableName);
        if (relation is null) return ([], null);

        var postgresString = selectModel.ToPostgreSqlString();
        var estimations = await _datasource.QueryAsync(dataSourceId, "SELECT * FROM query_stats(@Sql)", new
        {
            Sql = postgresString
        });

        var estimate = estimations.Single();
        if (!estimate.TryGetValue("cardinality", out var cardinalityObj))
            return ([], null);
        var cardinality = Convert.ToInt64(cardinalityObj);

        if (!estimate.TryGetValue("execution_time_ms", out var executionTimeMs))
            return ([], null);
        var cost = Convert.ToDouble(executionTimeMs) + relation.ConnectionOpenMs + relation.Transfer100Ms;

        var distribution =
            await _distributionUtils.GetAttributeDistributionsAsync(selectModel.Select
                .Select(x => (AttributeSpecifier)x).ToList());

        return ([
            new PlanContainer
            {
                Plan = new PushdownSqlPlanOperator(relation.DataSourceId, postgresString)
                {
                    Cost = cost,
                    ExpectedCardinality = cardinality,
                    Self = selectModel.From,
                    DistributionData = distribution,
                    Width = selectModel.Select.Count
                },
                PlannedItems = selectModel
            }
        ], null);
    }
}