using AlliumSativum.Connectors.PostgreSQL.Statistics;
using Grpc.Core;

namespace AlliumSativum.Worker.Services;

public sealed class MetricsService : Metrics.MetricsBase
{
    private readonly PostgreSqlStatistics _statistics;

    public MetricsService(PostgreSqlStatistics statistics)
    {
        _statistics = statistics;
    }
    
    public override async Task<Void> ScrapeMetrics(DataSourceSelector request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.DataSourceId, out var dataSource))
        {
            return new  Void();
        }
        
        await _statistics.ScrapeStatistics(dataSource);
        return new Void();
    }
}
