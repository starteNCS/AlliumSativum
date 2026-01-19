using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
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
    
    public static void ShouldBeVariableMapping(this VariableMappingSpecifier variableMappingSpecifier, string variableName, string attributeName)
    {
        variableMappingSpecifier.VariableName.Should().Be(variableName);
        variableMappingSpecifier.AttributeName.Should().Be(attributeName);
    }
    
    public static void ShouldContainAttributeSpecifier(this IList<ISpecifier> attributeSpecifiers, string dataSourceName, string tableName, string attributeName)
    {
        attributeSpecifiers.Should().Contain(attr => 
            attr is AttributeSpecifier &&
            ((AttributeSpecifier)attr).DataSourceName == dataSourceName &&
            ((AttributeSpecifier)attr).TableName == tableName &&
            ((AttributeSpecifier)attr).AttributeName == attributeName);
    }
    
    public static void ShouldContainVariableMappingSpecifier(this IList<ISpecifier> attributeSpecifiers, string alias, string attributeName)
    {
        attributeSpecifiers.Should().Contain(attr => 
            attr is VariableMappingSpecifier &&
            ((VariableMappingSpecifier)attr).VariableName == alias &&
            ((VariableMappingSpecifier)attr).AttributeName == attributeName);
    }
}
