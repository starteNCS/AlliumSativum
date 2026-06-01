namespace AlliumSativum.Worker.Sdk;

/// <summary>
///     Wrapper for calling the grpc metrics endpoints
/// </summary>
public sealed class MetricsApi
{
    private readonly Metrics.MetricsClient _metricsClient;

    public MetricsApi(Metrics.MetricsClient metricsClient)
    {
        _metricsClient = metricsClient;
    }

    /// <summary>
    ///     Triggert he metrics scraping for a given data source
    /// </summary>
    /// <param name="dataSource">Data source to scrape metrics of</param>
    public async Task TriggerMetricsScrapeAsync(Guid dataSource)
    {
        await _metricsClient.ScrapeMetricsAsync(new DataSourceSelector
        {
            DataSourceId = dataSource.ToString()
        });
    }
}