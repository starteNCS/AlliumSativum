namespace AlliumSativum.Worker.Sdk;

public sealed class MetricsApi
{
    private readonly Metrics.MetricsClient _metricsClient;

    public MetricsApi(Metrics.MetricsClient metricsClient)
    {
        _metricsClient = metricsClient;
    }

    public async Task TriggerMetricsScrapeAsync(Guid dataSource)
    {
        await _metricsClient.ScrapeMetricsAsync(new DataSourceSelector
        {
            DataSourceId = dataSource.ToString()
        });
    }
}