using AlliumSativum.Optimize;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;

namespace QueryPlanner.Tests.Optimize.ExpressionNode;

public sealed class AttributeExtractionTests
{
    private static readonly ExpressionNodeOptimizer ExpressionOptimizer = new();

    [Test]
    public void Should_Extract_Attributes_From_Simple_Expression()
    {
        var expression = new BinaryOperatorExpressionNode
        {
            Left = new FullySpecifiedColumnExpressionNode
            {
                Attribute = new AttributeSpecifier("ticket", "tickets", "subject")
            },
            Operation = "=",
            Right = new ValueExpressionNode
            {
                Value = "value",
                Type = ValueExpressionNode.ValueExpressionType.String
            }
        };

        var attributes = expression.GetAttributesOfExpression();

        attributes.Should().NotBeEmpty()
            .And.HaveCount(1)
            .And.Contain(new AttributeSpecifier("ticket", "tickets", "subject"));
    }

    [Test]
    public void Should_Extract_Tables_From_Complex_Expression()
    {
        var expression = new BinaryOperatorExpressionNode
        {
            Left = new FullySpecifiedColumnExpressionNode
            {
                Attribute = new AttributeSpecifier("ticket", "tickets", "subject")
            },
            Operation = "AND",
            Right = new FullySpecifiedColumnExpressionNode
            {
                Attribute = new AttributeSpecifier("erp", "employee", "name")
            }
        };

        var tables = expression.GetTablesOfExpression();

        tables.Should().NotBeEmpty()
            .And.HaveCount(2)
            .And.Contain(new TableSpecifier("ticket", "tickets"))
            .And.Contain(new TableSpecifier("erp", "employee"));
    }

    [Test]
    public void Should_Handle_Empty_Expression_For_Attributes()
    {
        var expression = new ValueExpressionNode
        {
            Value = string.Empty,
            Type = ValueExpressionNode.ValueExpressionType.String
        };

        var attributes = expression.GetAttributesOfExpression();

        attributes.Should().BeEmpty();
    }

    [Test]
    public void Should_Handle_Empty_Expression_For_Tables()
    {
        var expression = new ValueExpressionNode
        {
            Value = string.Empty,
            Type = ValueExpressionNode.ValueExpressionType.String
        };

        var tables = expression.GetTablesOfExpression();

        tables.Should().BeEmpty();
    }

    [Test]
    public void Should_Extract_Attributes_From_Complex_Expression_With_And_Or()
    {
        var expression = new BinaryOperatorExpressionNode
        {
            Left = new BinaryOperatorExpressionNode
            {
                Left = new FullySpecifiedColumnExpressionNode
                {
                    Attribute = new AttributeSpecifier("ticket", "tickets", "subject")
                },
                Operation = "AND",
                Right = new FullySpecifiedColumnExpressionNode
                {
                    Attribute = new AttributeSpecifier("erp", "employee", "name")
                }
            },
            Operation = "OR",
            Right = new FullySpecifiedColumnExpressionNode
            {
                Attribute = new AttributeSpecifier("crm", "customers", "email")
            }
        };

        var attributes = expression.GetAttributesOfExpression();

        attributes.Should().NotBeEmpty()
            .And.HaveCount(3)
            .And.Contain(new AttributeSpecifier("ticket", "tickets", "subject"))
            .And.Contain(new AttributeSpecifier("erp", "employee", "name"))
            .And.Contain(new AttributeSpecifier("crm", "customers", "email"));
    }
}
