namespace AlliumSavitum.Connectors.Shared.Interfaces;

public interface IDataSourceStatistics
{
    Task ScrapeStatistics(Guid dataSource);
    
    double GetCardinalityOfTable(Guid dataSource, string table);
    double GetUpperBoundSizeOfTable(Guid dataSource, string table);
    double GetUpperBoundSizeOfTable(Guid dataSource, string table, List<string> columns);
}
