namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

public class DataSourceSpecifier : ISpecifier, IEquatable<DataSourceSpecifier>
{
    public DataSourceSpecifier(string dataSourceNameName)
    {
        DataSourceName = dataSourceNameName;
    }

    public string DataSourceName { get; }

    public bool Equals(DataSourceSpecifier? other)
    {
        return other != null &&
               DataSourceName == other.DataSourceName;
    }

    public override string ToString()
    {
        return DataSourceName;
    }

    public TableSpecifier ToTableSpecifier(string tableName)
    {
        return new TableSpecifier(DataSourceName, tableName);
    }


    public override int GetHashCode()
    {
        return DataSourceName.GetHashCode();
    }
}