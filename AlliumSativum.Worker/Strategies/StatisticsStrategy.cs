using AlliumSativum.Connectors.PostgreSQL.Statistics;
using AlliumSativum.Connectors.TicketSystem.Statistics;
using AlliumSativum.Shared.Enums;
using AlliumSavitum.Connectors.Shared.Interfaces;

namespace AlliumSativum.Worker.Strategies;

public sealed class StatisticsStrategy
{
    private readonly PostgreSqlStatistics _postgreSqlStatistics;
    private readonly TicketSystemStatistics _ticketSystemStatistics;

    public StatisticsStrategy(
        PostgreSqlStatistics  postgreSqlStatistics,
        TicketSystemStatistics ticketSystemStatistics)
    {
        _postgreSqlStatistics = postgreSqlStatistics;
        _ticketSystemStatistics = ticketSystemStatistics;
    }

    public IDataSourceStatistics GetStatisticsOfConnector(ConnectorType connectorType)
    {
        return connectorType switch
        {
            ConnectorType.Postgres => _postgreSqlStatistics,
            ConnectorType.TicketSystem => _ticketSystemStatistics,
            _ => throw new ArgumentException("Invalid connector type. Did you forget to add it to the strategy?", nameof(connectorType))
        };
    }
}
