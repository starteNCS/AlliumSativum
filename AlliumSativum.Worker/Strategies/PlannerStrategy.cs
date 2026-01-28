using AlliumSativum.Connectors.PostgreSQL.Planners;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Enums;

namespace AlliumSativum.Worker.Strategies;

public sealed class PlannerStrategy
{
    private readonly PostgreSqlPlanner _postgresPlanner;

    public PlannerStrategy(PostgreSqlPlanner postgresPlanner)
    {
        _postgresPlanner = postgresPlanner;
    }

    public IPlanner GetPlannerOfConnector(ConnectorType connectorType)
    {
        return connectorType switch
        {
            ConnectorType.Postgres => _postgresPlanner,
            _ => throw new ArgumentException("Invalid connector type", nameof(connectorType))
        };
    }
}
