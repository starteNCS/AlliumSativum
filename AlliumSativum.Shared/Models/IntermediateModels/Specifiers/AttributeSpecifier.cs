namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

public sealed class AttributeSpecifier : TableSpecifier
{
    public string AttributeName { get; set; }

    public AttributeSpecifier(string dataSourceName, string tableName, string attributeName) : base(dataSourceName, tableName)
    {
        AttributeName = attributeName;
    }

    public bool IsInTable(TableSpecifier table)
    {
        return DataSourceName == table.DataSourceName && TableName == table.TableName;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not AttributeSpecifier other)
        {
            return false;
        }
        
        return DataSourceName == other.DataSourceName &&  TableName == other.TableName && AttributeName == other.AttributeName;
    }
}