using AlliumSativum.Shared.Constants;

namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

public sealed class AttributeSpecifier : TableSpecifier, IEquatable<AttributeSpecifier>
{
    public string AttributeName { get; }
    
    /// <summary>
    /// This flag controls whether the attribute is used only for calculations (i.e. joins)
    /// or if the attribute should be output
    /// </summary>
    public bool IsHidden { get; set; }

    public AttributeSpecifier(string dataSourceName, string tableName, string attributeName) : base(dataSourceName, tableName)
    {
        AttributeName = attributeName;
    }
    
    public override string ToString() => $"{base.ToString()}{AsSqlParameters.Attribute.TableSeparator}{AttributeName}";

    public bool IsInTable(TableSpecifier table)
    {
        return DataSourceName == table.DataSourceName && TableName == table.TableName;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(DataSourceName, TableName, AttributeName);
    }

    public bool Equals(AttributeSpecifier? other) =>
        other != null &&
        DataSourceName == other.DataSourceName &&
        TableName == other.TableName &&
        AttributeName == other.AttributeName;
}