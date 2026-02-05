namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

public class DataSourceSpecifier : ISpecifier, IEquatable<DataSourceSpecifier>
{
    public string DataSourceName { get; }

    public DataSourceSpecifier(string dataSourceNameName)
    {
        DataSourceName = dataSourceNameName;
    }

    public override string ToString() => DataSourceName;

    public TableSpecifier ToTableSpecifier(string tableName)
    {
        return new TableSpecifier(DataSourceName, tableName);
    }
    
    public bool Equals(DataSourceSpecifier? other) =>
        other != null &&
        DataSourceName == other.DataSourceName;


    public override int GetHashCode()
    {
        return DataSourceName.GetHashCode();
    }
}