namespace AlliumSavitum.Connectors.Shared.Interfaces;

public interface IDataSourceStatistics
{
    Task ScrapeStatistics(Guid dataSource);
}
