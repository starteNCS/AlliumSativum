using AlliumSativum.Connectors.PostgreSQL.Planners;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Connectors.TicketSystem.Planner;
using AlliumSativum.Shared.Enums;

namespace AlliumSativum.Worker.Strategies;

public sealed class PlannerStrategy
{
    private readonly PostgreSqlPlanner _postgresPlanner;
    private readonly TicketSystemPlanner _ticketSystemPlanner;

    public PlannerStrategy(
        PostgreSqlPlanner postgresPlanner,
        TicketSystemPlanner ticketSystemPlanner)
    {
        _postgresPlanner = postgresPlanner;
        _ticketSystemPlanner = ticketSystemPlanner;
    }

    public IPlanner GetPlannerOfConnector(ConnectorType connectorType)
    {
        return connectorType switch
        {
            ConnectorType.Postgres => _postgresPlanner,
            ConnectorType.TicketSystem => _ticketSystemPlanner,
            _ => throw new ArgumentException("Invalid connector type. Did you forget to add it to the strategy?", nameof(connectorType))
        };
    }
}
