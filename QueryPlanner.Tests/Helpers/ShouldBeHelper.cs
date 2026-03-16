using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;

namespace QueryPlanner.Tests.Helpers;

public static partial class ShouldBeHelper
{
    public static void ShouldBeAttribute(this AttributeSpecifier attributeSpecifier, string dataSourceName,
        string tableName, string attributeName, bool isHidden = false)
    {
        attributeSpecifier.DataSourceName.Should().Be(dataSourceName);
        attributeSpecifier.TableName.Should().Be(tableName);
        attributeSpecifier.AttributeName.Should().Be(attributeName);
    }

    public static void ShouldBeVariableMapping(this VariableMappingSpecifier variableMappingSpecifier,
        string variableName, string attributeName, bool isHidden = false)
    {
        variableMappingSpecifier.VariableName.Should().Be(variableName);
        variableMappingSpecifier.AttributeName.Should().Be(attributeName);
    }


    public static void ShouldContainVariableMappingSpecifier(this IList<ISpecifier> attributeSpecifiers, string alias,
        string attributeName)
    {
        attributeSpecifiers.Should().Contain(attr =>
            attr is VariableMappingSpecifier &&
            ((VariableMappingSpecifier)attr).VariableName == alias &&
            ((VariableMappingSpecifier)attr).AttributeName == attributeName);
    }

    extension(TableSpecifier tableSpecifier)
    {
        public void ShouldBeTable(string dataSourceName, string tableName)
        {
            tableSpecifier.DataSourceName.Should().Be(dataSourceName);
            tableSpecifier.TableName.Should().Be(tableName);
        }

        public void ShouldBeTable(TableSpecifier other)
        {
            tableSpecifier.DataSourceName.Should().Be(other.DataSourceName);
            tableSpecifier.TableName.Should().Be(other.TableName);
        }
    }

    extension(IList<ISpecifier> attributeSpecifiers)
    {
        public void ShouldContainAttributeSpecifier(string dataSourceName, string tableName, string attributeName,
            bool isHidden = false)
        {
            attributeSpecifiers.All(attr => attr is AttributeSpecifier).Should().BeTrue();
            var attrs = attributeSpecifiers.Select(attr => (AttributeSpecifier)attr).ToList();
            attrs.ShouldContainAttributeSpecifier(dataSourceName, tableName, attributeName);
        }

        public void ShouldContainAttributeSpecifier(AttributeSpecifier attributeSpecifier, bool isHidden = false)
        {
            attributeSpecifiers.ShouldContainAttributeSpecifier(attributeSpecifier.DataSourceName,
                attributeSpecifier.TableName, attributeSpecifier.AttributeName);
        }
    }

    extension(IList<AttributeSpecifier> attributeSpecifiers)
    {
        public void ShouldContainAttributeSpecifier(string dataSourceName, string tableName, string attributeName)
        {
            attributeSpecifiers.Should().Contain(attr =>
                attr.DataSourceName == dataSourceName &&
                attr.TableName == tableName &&
                attr.AttributeName == attributeName);
        }

        public void ShouldContainAttributeSpecifier(AttributeSpecifier attributeSpecifier)
        {
            attributeSpecifiers.ShouldContainAttributeSpecifier(attributeSpecifier.DataSourceName,
                attributeSpecifier.TableName, attributeSpecifier.AttributeName);
        }
    }
}