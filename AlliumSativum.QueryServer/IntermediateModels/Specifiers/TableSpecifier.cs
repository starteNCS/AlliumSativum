namespace AlliumSativum.IntermediateModels;

public class TableSpecifier : DataSourceSpecifier
{
    public string TableName { get; set; }

    public TableSpecifier(string dataSourceName, string tableName) : base(dataSourceName)
    {
        TableName = tableName;
    }
}