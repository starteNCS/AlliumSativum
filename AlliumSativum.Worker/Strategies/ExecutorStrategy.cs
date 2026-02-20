using AlliumSativum.Connectors.PostgreSQL.Executor;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Enums;

namespace AlliumSativum.Worker.Strategies;

public sealed class ExecutorStrategy
{
    private readonly PostgreSqlExecutor _postgreSqlExecutor;

    public ExecutorStrategy(PostgreSqlExecutor postgreSqlExecutor)
    {
        _postgreSqlExecutor = postgreSqlExecutor;
    }

    public IWorkerExecutor GetPlannerOfConnector(ConnectorType connectorType)
    {
        return connectorType switch
        {
            ConnectorType.Postgres => _postgreSqlExecutor,
            ConnectorType.TicketSystem => null!,
            _ => throw new ArgumentException("Invalid connector type. Did you forget to add it to the strategy?", nameof(connectorType))
        };
    }
}
