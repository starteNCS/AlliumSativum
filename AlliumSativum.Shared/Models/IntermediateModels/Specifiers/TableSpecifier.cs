namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

public class TableSpecifier : DataSourceSpecifier
{
    public string TableName { get; set; }

    public TableSpecifier(string dataSourceName, string tableName) : base(dataSourceName)
    {
        TableName = tableName;
    }

    public AttributeSpecifier ToAttributeSpecifier(string attributeName)
    {
        return new AttributeSpecifier(DataSourceName, TableName, attributeName);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not TableSpecifier other)
        {
            return false;
        }
        
        return DataSourceName == other.DataSourceName &&  TableName == other.TableName;
    }
}