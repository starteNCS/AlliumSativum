using AlliumSativum.Shared.Constants;

namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

public class TableSpecifier : DataSourceSpecifier, IEquatable<TableSpecifier>
{
    public string TableName { get; }

    public TableSpecifier(string dataSourceName, string tableName) : base(dataSourceName)
    {
        TableName = tableName;
    }

    public override string ToString() => $"{base.ToString()}{AsSqlParameters.Attribute.DataSourceSeparator}{TableName}";

    public AttributeSpecifier ToAttributeSpecifier(string attributeName)
    {
        return new AttributeSpecifier(DataSourceName, TableName, attributeName);
    }

    public bool Equals(TableSpecifier? other) =>
        other != null &&
        DataSourceName == other.DataSourceName &&
        TableName == other.TableName;

    public override int GetHashCode()
    {
        return HashCode.Combine(DataSourceName, TableName);
    }
}