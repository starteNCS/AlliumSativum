namespace AlliumSavitum.Connectors.Shared.Interfaces;

public interface IDataSourceStatistics
{
    Task ScrapeStatisticsAsync(Guid dataSource);
}