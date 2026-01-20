using Grpc.Core;

namespace AlliumSativum.Worker.Services;

public sealed class MetricsService : Metrics.MetricsBase
{
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
    }
    
    public override async Task<Void> ScrapeMetrics(DataSourceSelector request, ServerCallContext context)
    {
        _logger.LogInformation("ScrapeMetrics called");
        return new Void();
    }
}
