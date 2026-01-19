namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

public sealed class AttributeSpecifier : TableSpecifier
{
    public string AttributeName { get; set; }

    public AttributeSpecifier(string dataSourceName, string tableName, string attributeName) : base(dataSourceName, tableName)
    {
        AttributeName = attributeName;
    }
}