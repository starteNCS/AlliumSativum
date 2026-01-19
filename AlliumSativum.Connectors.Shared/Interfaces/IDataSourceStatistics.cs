namespace AlliumSavitum.Connectors.Shared.Interfaces;

public interface IDataSourceStatistics
{
    Task ScrapeStatistics(string dataSource);
    
    double GetCardinalityOfTable(string dataSource, string table);
    double GetUpperBoundSizeOfTable(string dataSource, string table);
    double GetUpperBoundSizeOfTable(string dataSource, string table, List<string> columns);
}
