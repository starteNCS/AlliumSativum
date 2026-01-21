using AlliumSativum.Connectors.PostgreSQL.Statistics;
using Grpc.Core;

namespace AlliumSativum.Worker.Services;

public sealed class MetricsService : Metrics.MetricsBase
{
    private readonly ILogger<MetricsService> _logger;
    private readonly PostgreSqlStatistics _statistics;

    public MetricsService(
        ILogger<MetricsService> logger,
        PostgreSqlStatistics statistics)
    {
        _logger = logger;
        _statistics = statistics;
    }
    
    public override async Task<Void> ScrapeMetrics(DataSourceSelector request, ServerCallContext context)
    {
        await _statistics.ScrapeStatistics("asdf");
        return new Void();
    }
}
