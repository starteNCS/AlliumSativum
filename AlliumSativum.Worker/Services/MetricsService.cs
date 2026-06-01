using System.Diagnostics;
using AlliumSativum.Shared.Database;
using AlliumSativum.Worker.Strategies;
using Grpc.Core;

namespace AlliumSativum.Worker.Services;

/// <summary>
///     gRPC endpoint for metric scraping a data source
/// </summary>
public sealed class MetricsService : Metrics.MetricsBase
{
    private readonly CatalogDatabase _catalog;
    private readonly ILogger<MetricsService> _logger;
    private readonly StatisticsStrategy _statisticsStrategy;

    public MetricsService(
        StatisticsStrategy statisticsStrategy,
        CatalogDatabase catalog,
        ILogger<MetricsService> logger)
    {
        _statisticsStrategy = statisticsStrategy;
        _catalog = catalog;
        _logger = logger;
    }

    public override async Task<Void> ScrapeMetrics(DataSourceSelector request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.DataSourceId, out var dataSourceId)) return new Void();

        var datasource = await _catalog.GetDataSourceAsync(dataSourceId);
        if (datasource == null) return new Void();

        var statistics = _statisticsStrategy.GetStatisticsOfConnector(datasource.Connector);
        var stopwatch = Stopwatch.StartNew();
        await statistics.ScrapeStatisticsAsync(datasource.Id);
        stopwatch.Stop();
        _logger.LogInformation("Scrape statistics for {DataSource} took {StopwatchElapsedMilliseconds}ms",
            datasource.Name, stopwatch.ElapsedMilliseconds);
        return new Void();
    }
}