namespace AlliumSavitum.Connectors.Shared.Interfaces;

public interface IDataSourceStatistics
{
    /// <summary>
    ///     Scrapes the statistics for a given data source and updates the catalog with the new information
    /// </summary>
    /// <param name="dataSource">Data source to scrape</param>
    Task ScrapeStatisticsAsync(Guid dataSource);
}