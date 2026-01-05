using AlliumSativum.IntermediateModels;
using FluentAssertions;

namespace ParserTests.Helpers;

public static class ShouldBeHelper
{
    public static void ShouldBeTable(this TableSpecifier tableSpecifier, string dataSourceName, string tableName)
    {
        tableSpecifier.DataSourceName.Should().Be(dataSourceName);
        tableSpecifier.TableName.Should().Be(tableName);
    }

    public static void ShouldContainAttribute(this IList<AttributeSpecifier> attributeSpecifiers, string dataSourceName, string tableName, string attributeName)
    {
        attributeSpecifiers.Should().Contain(attr => attr.DataSourceName == dataSourceName && attr.TableName == tableName && attr.AttributeName == attributeName);
    }
}
