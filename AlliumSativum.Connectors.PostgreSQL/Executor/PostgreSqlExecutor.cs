using System.Diagnostics;
using AlliumSativum.Connectors.PostgreSQL.DatabaseConnectors;
using AlliumSativum.Connectors.PostgreSQL.Statistics;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Enums;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.Executor;
using Microsoft.Extensions.Logging;

namespace AlliumSativum.Connectors.PostgreSQL.Executor;

public sealed class PostgreSqlExecutor : IWorkerExecutor
{
    private readonly DatasourceDatabase _dataSource;

    public PostgreSqlExecutor(DatasourceDatabase dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<ExecutorWrapper> ExecuteAsync(PlanOperator @operator)
    {
        if (@operator is not PushdownSqlPlanOperator pushdown)
        {
            throw new AsSQLExecuteException("Invalid plan operator type for PostgreSqlExecutor. Expected PushdownSqlPlanOperator.", ConnectorType.Postgres);
        }
        
        var stopwatch = Stopwatch.StartNew();
        var result = await _dataSource.QueryAsync<object>(@pushdown.DataSource, @pushdown.SqlStatement);
        stopwatch.Stop();
        
        return new ExecutorWrapper
        {
            PlanOperator = @operator,
            Result = result,
            FactualCardinality = result.Count,
            FactualCost = stopwatch.ElapsedMilliseconds
        };
    }
}
