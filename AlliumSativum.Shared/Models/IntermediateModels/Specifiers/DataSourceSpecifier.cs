namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

public class DataSourceSpecifier : ISpecifier
{
    public string DataSourceName { get; set; }

    public DataSourceSpecifier(string dataSourceNameName)
    {
        DataSourceName = dataSourceNameName;
    }

    public TableSpecifier ToTableSpecifier(string tableName)
    {
        return new TableSpecifier(DataSourceName, tableName);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not DataSourceSpecifier other)
        {
            return false;
        }
        
        return DataSourceName == other.DataSourceName;
    }
}