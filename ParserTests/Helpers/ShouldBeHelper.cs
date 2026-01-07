using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;
using FluentAssertions;

namespace ParserTests.Helpers;

public static partial class ShouldBeHelper
{
    public static void ShouldBeTable(this TableSpecifier tableSpecifier, string dataSourceName, string tableName)
    {
        tableSpecifier.DataSourceName.Should().Be(dataSourceName);
        tableSpecifier.TableName.Should().Be(tableName);
    }

    public static void ShouldBeAttribute(this AttributeSpecifier attributeSpecifier, string dataSourceName, string tableName, string attributeName)
    {
        attributeSpecifier.DataSourceName.Should().Be(dataSourceName);
        attributeSpecifier.TableName.Should().Be(tableName);
        attributeSpecifier.AttributeName.Should().Be(attributeName);
    }
    
    public static void ShouldContainAttributeSpecifier(this IList<ISpecifier> attributeSpecifiers, string dataSourceName, string tableName, string attributeName)
    {
        attributeSpecifiers.Should().Contain(attr => 
            attr is AttributeSpecifier &&
            ((AttributeSpecifier)attr).DataSourceName == dataSourceName &&
            ((AttributeSpecifier)attr).TableName == tableName &&
            ((AttributeSpecifier)attr).AttributeName == attributeName);
    }
}
