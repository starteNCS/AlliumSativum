using AlliumSativum.Connectors.JsonServer.Executor;
using AlliumSativum.Connectors.PostgreSQL.Executor;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Enums;

namespace AlliumSativum.Worker.Strategies;

public sealed class ExecutorStrategy
{
    private readonly PostgreSqlExecutor _postgreSqlExecutor;
    private readonly JsonServerExecutor _jsonServerExecutor;

    public ExecutorStrategy(PostgreSqlExecutor postgreSqlExecutor, JsonServerExecutor jsonServerExecutor)
    {
        _postgreSqlExecutor = postgreSqlExecutor;
        _jsonServerExecutor = jsonServerExecutor;
    }

    public IWorkerExecutor GetPlannerOfConnector(ConnectorType connectorType)
    {
        return connectorType switch
        {
            ConnectorType.Postgres => _postgreSqlExecutor,
            ConnectorType.JsonServer => _jsonServerExecutor,
            _ => throw new ArgumentException("Invalid connector type. Did you forget to add it to the strategy?", nameof(connectorType))
        };
    }
}
