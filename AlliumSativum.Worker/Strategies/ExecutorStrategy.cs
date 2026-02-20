using AlliumSativum.Connectors.PostgreSQL.Executor;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Connectors.TicketSystem.Executor;
using AlliumSativum.Shared.Enums;

namespace AlliumSativum.Worker.Strategies;

public sealed class ExecutorStrategy
{
    private readonly PostgreSqlExecutor _postgreSqlExecutor;
    private readonly TicketSystemExecutor _ticketSystemExecutor;

    public ExecutorStrategy(PostgreSqlExecutor postgreSqlExecutor, TicketSystemExecutor ticketSystemExecutor)
    {
        _postgreSqlExecutor = postgreSqlExecutor;
        _ticketSystemExecutor = ticketSystemExecutor;
    }

    public IWorkerExecutor GetPlannerOfConnector(ConnectorType connectorType)
    {
        return connectorType switch
        {
            ConnectorType.Postgres => _postgreSqlExecutor,
            ConnectorType.TicketSystem => _ticketSystemExecutor,
            _ => throw new ArgumentException("Invalid connector type. Did you forget to add it to the strategy?", nameof(connectorType))
        };
    }
}
