using AlliumSativum.Connectors.JsonServer.Planner;
using AlliumSativum.Connectors.PostgreSQL.Planners;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Enums;

namespace AlliumSativum.Worker.Strategies;

public sealed class PlannerStrategy
{
    private readonly PostgreSqlPlanner _postgresPlanner;
    private readonly JsonServerPlanner _jsonServerPlanner;

    public PlannerStrategy(
        PostgreSqlPlanner postgresPlanner,
        JsonServerPlanner jsonServerPlanner)
    {
        _postgresPlanner = postgresPlanner;
        _jsonServerPlanner = jsonServerPlanner;
    }

    public IPlanner GetPlannerOfConnector(ConnectorType connectorType)
    {
        return connectorType switch
        {
            ConnectorType.Postgres => _postgresPlanner,
            ConnectorType.JsonServer => _jsonServerPlanner,
            _ => throw new ArgumentException("Invalid connector type. Did you forget to add it to the strategy?", nameof(connectorType))
        };
    }
}
