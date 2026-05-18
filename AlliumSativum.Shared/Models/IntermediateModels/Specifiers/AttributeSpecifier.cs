using System.Text;
using AlliumSativum.Shared.Constants;

namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

/// <summary>
/// Specifier for the full attribute
/// </summary>
public sealed class AttributeSpecifier : TableSpecifier, IEquatable<AttributeSpecifier>
{
    public AttributeSpecifier(string dataSourceName, string tableName, string attributeName) : base(dataSourceName,
        tableName)
    {
        AttributeName = attributeName;
    }

    public string AttributeName { get; }

    /// <summary>
    ///     This flag controls whether the attribute is used only for calculations (i.e. joins)
    ///     or if the attribute should be output
    /// </summary>
    public bool IsHidden { get; set; }

    public TableSpecifier Table => new(DataSourceName, TableName);
    
    public bool Equals(AttributeSpecifier? other)
    {
        return other != null &&
               DataSourceName == other.DataSourceName &&
               TableName == other.TableName &&
               AttributeName == other.AttributeName;
    }

    public override bool Equals(object? obj)
    {
        if (obj is AttributeSpecifier other)
        {
            return Equals(other);
        }
        return false;
    }

    public override string ToString()
    {
        return new StringBuilder()
            .Append(IsHidden ? "[HIDDEN] " : "")
            .Append(base.ToString())
            .Append(AsSqlParameters.Attribute.TableSeparator)
            .Append(AttributeName)
            .ToString();
    }

    public string ToDictKey()
    {
        return new StringBuilder()
            .Append(base.ToString())
            .Append(AsSqlParameters.Attribute.TableSeparator)
            .Append(AttributeName)
            .ToString();
    }


    public bool IsInTable(TableSpecifier table)
    {
        return DataSourceName == table.DataSourceName && TableName == table.TableName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DataSourceName, TableName, AttributeName);
    }
}