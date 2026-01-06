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
    
    public static void ShouldContainAttribute(this IList<AttributeSpecifier> attributeSpecifiers, string dataSourceName, string tableName, string attributeName)
    {
        attributeSpecifiers.Should().Contain(attr => attr.DataSourceName == dataSourceName && attr.TableName == tableName && attr.AttributeName == attributeName);
    }
}
