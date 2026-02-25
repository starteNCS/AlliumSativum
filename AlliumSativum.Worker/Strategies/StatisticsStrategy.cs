using AlliumSativum.Connectors.JsonServer.Statistics;
using AlliumSativum.Connectors.PostgreSQL.Statistics;
using AlliumSativum.Shared.Enums;
using AlliumSavitum.Connectors.Shared.Interfaces;

namespace AlliumSativum.Worker.Strategies;

public sealed class StatisticsStrategy
{
    private readonly PostgreSqlStatistics _postgreSqlStatistics;
    private readonly JsonServerStatistics _jsonServerStatistics;

    public StatisticsStrategy(
        PostgreSqlStatistics  postgreSqlStatistics,
        JsonServerStatistics jsonServerStatistics)
    {
        _postgreSqlStatistics = postgreSqlStatistics;
        _jsonServerStatistics = jsonServerStatistics;
    }

    public IDataSourceStatistics GetStatisticsOfConnector(ConnectorType connectorType)
    {
        return connectorType switch
        {
            ConnectorType.Postgres => _postgreSqlStatistics,
            ConnectorType.JsonServer => _jsonServerStatistics,
            _ => throw new ArgumentException("Invalid connector type. Did you forget to add it to the strategy?", nameof(connectorType))
        };
    }
}
