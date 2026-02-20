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
        if (@operator is not PushdownSqlPlanOperator pushdownPop)
        {
            throw new AsSQLExecuteException("Unsupported plan operator type", ConnectorType.Postgres);
        }

        var stopwatch = Stopwatch.StartNew();
        var result = await _dataSource.QueryAsync<object>(pushdownPop.DataSource, pushdownPop.SqlStatement);
        stopwatch.Stop();
        
        return new ExecutorWrapper
        {
            PlanOperator = pushdownPop,
            Result = result,
            FactualCardinality = result.Count,
            FactualCost = stopwatch.ElapsedMilliseconds
        };
    }
}
