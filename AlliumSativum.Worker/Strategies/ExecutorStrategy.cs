using AlliumSativum.Connectors.JsonServer.Executor;
using AlliumSativum.Connectors.PostgreSQL.Executor;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Enums;

namespace AlliumSativum.Worker.Strategies;

public sealed class ExecutorStrategy
{
    private readonly JsonServerExecutor _jsonServerExecutor;
    private readonly PostgreSqlExecutor _postgreSqlExecutor;

    public ExecutorStrategy(PostgreSqlExecutor postgreSqlExecutor, JsonServerExecutor jsonServerExecutor)
    {
        _postgreSqlExecutor = postgreSqlExecutor;
        _jsonServerExecutor = jsonServerExecutor;
    }

    /// <summary>
    /// Based on the connector type, it returns the appropriate worker executor
    /// </summary>
    /// <param name="connectorType">The connector type needed for the data source</param>
    /// <returns>The correct executor</returns>
    /// <exception cref="ArgumentException">Invlaid connector type</exception>
    public IWorkerExecutor GetPlannerOfConnector(ConnectorType connectorType)
    {
        return connectorType switch
        {
            ConnectorType.Postgres => _postgreSqlExecutor,
            ConnectorType.JsonServer => _jsonServerExecutor,
            _ => throw new ArgumentException("Invalid connector type. Did you forget to add it to the strategy?",
                nameof(connectorType))
        };
    }
}