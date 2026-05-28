using AlliumSativum.Parser.Algorithms;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Token;
using FluentAssertions;
using Test.Shared.Helpers;

namespace QueryPlanner.Tests.Algorithms;

public sealed class BooleanExpressionParserTest
{
    private static readonly Tokenizer Tokenizer = new();

    #region NegativeTests

    [Test]
    public void ShouldHandle_ParenthesesMismatch()
    {
        var expression = "(erp->customers.name = 'test'";
        var tokens = Tokenizer.Tokenize(expression);
        Action action = () => BooleanExpressionParser.Parse(tokens);
        action.ShouldThrowParseException("", "Mismatched parentheses.");
    }

    #endregion

    #region PostiveTests

    [Test]
    public void ShouldHandle_SingleWhere_Value()
    {
        var expression = "erp->customers.name = 'test'";
        var tokens = Tokenizer.Tokenize(expression);
        var result = BooleanExpressionParser.Parse(tokens);

        result.Should().NotBeNull();
        result.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "test");
    }

    [Test]
    public void ShouldHandle_SingleWhere_Attribute()
    {
        var expression = "erp->customers.name = erp->orders.ordered_by";
        var tokens = Tokenizer.Tokenize(expression);
        var result = BooleanExpressionParser.Parse(tokens);

        result.Should().NotBeNull();
        result.ShouldBeBinaryOperator("=",
            new AttributeSpecifier("erp", "customers", "name"),
            new AttributeSpecifier("erp", "orders", "ordered_by"));
    }

    [Test]
    public void ShouldHandle_AndWhere()
    {
        var expression = "erp->customers.name = 'test' AND erp->customers.number = 1234";
        var tokens = Tokenizer.Tokenize(expression);
        var result = BooleanExpressionParser.Parse(tokens);

        result.Should().NotBeNull();
        result.ShouldBeBinaryOperator("AND");
        var op = (BinaryOperatorExpressionNode)result;
        op.Left.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "test");
        op.Right.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "number"), "1234");
    }

    [Test]
    public void ShouldHandle_OrWhere()
    {
        var expression = "erp->customers.name = 'test' OR erp->customers.number = 1234";
        var tokens = Tokenizer.Tokenize(expression);
        var result = BooleanExpressionParser.Parse(tokens);

        result.Should().NotBeNull();
        result.ShouldBeBinaryOperator("OR");
        var op = (BinaryOperatorExpressionNode)result;
        op.Left.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "test");
        op.Right.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "number"), "1234");
    }

    [Test]
    public void ShouldHandle_AndOrWhere()
    {
        var expression =
            "erp->customers.name = erp->orders.ordered_by AND erp->customers.name = 'test' OR erp->customers.number = 1234";
        var tokens = Tokenizer.Tokenize(expression);
        var result = BooleanExpressionParser.Parse(tokens);

        result.Should().NotBeNull();
        result.ShouldBeBinaryOperator("OR");
        var op = (BinaryOperatorExpressionNode)result;
        op.Left.ShouldBeBinaryOperator("AND");
        var leftOp = (BinaryOperatorExpressionNode)op.Left;
        leftOp.Left.ShouldBeBinaryOperator("=",
            new AttributeSpecifier("erp", "customers", "name"),
            new AttributeSpecifier("erp", "orders", "ordered_by"));
        leftOp.Right.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "test");

        op.Right.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "number"), "1234");
    }

    [Test]
    public void ShouldHandle_AndOrParenthesesWhere()
    {
        var expression =
            "erp->customers.name = erp->orders.ordered_by AND (erp->customers.name = 'test' OR erp->customers.number = 1234)";
        var tokens = Tokenizer.Tokenize(expression);
        var result = BooleanExpressionParser.Parse(tokens);

        result.Should().NotBeNull();
        result.ShouldBeBinaryOperator("AND");
        var op = (BinaryOperatorExpressionNode)result;
        op.Left.ShouldBeBinaryOperator("=",
            new AttributeSpecifier("erp", "customers", "name"),
            new AttributeSpecifier("erp", "orders", "ordered_by"));

        op.Right.ShouldBeBinaryOperator("OR");
        var rightOp = (BinaryOperatorExpressionNode)op.Right;
        rightOp.Left.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "test");

        rightOp.Right.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "number"), "1234");
    }

    #endregion
}