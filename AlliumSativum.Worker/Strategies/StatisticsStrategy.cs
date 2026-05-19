using AlliumSativum.Connectors.JsonServer.Statistics;
using AlliumSativum.Connectors.PostgreSQL.Statistics;
using AlliumSativum.Shared.Enums;
using AlliumSavitum.Connectors.Shared.Interfaces;

namespace AlliumSativum.Worker.Strategies;

public sealed class StatisticsStrategy
{
    private readonly JsonServerStatistics _jsonServerStatistics;
    private readonly PostgreSqlStatistics _postgreSqlStatistics;

    public StatisticsStrategy(
        PostgreSqlStatistics postgreSqlStatistics,
        JsonServerStatistics jsonServerStatistics)
    {
        _postgreSqlStatistics = postgreSqlStatistics;
        _jsonServerStatistics = jsonServerStatistics;
    }

    /// <summary>
    /// Based on the connector type, it returns the appropriate statistics scraper
    /// </summary>
    /// <param name="connectorType">The connector type needed for the data source</param>
    /// <returns>The correct statistics scraper</returns>
    /// <exception cref="ArgumentException">Invlaid connector type</exception>
    public IDataSourceStatistics GetStatisticsOfConnector(ConnectorType connectorType)
    {
        return connectorType switch
        {
            ConnectorType.Postgres => _postgreSqlStatistics,
            ConnectorType.JsonServer => _jsonServerStatistics,
            _ => throw new ArgumentException("Invalid connector type. Did you forget to add it to the strategy?",
                nameof(connectorType))
        };
    }
}