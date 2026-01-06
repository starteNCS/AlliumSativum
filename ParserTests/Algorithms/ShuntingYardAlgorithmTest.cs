using AlliumSativum.Parser.Algorithms;
using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;
using FluentAssertions;
using NUnit.Framework.Constraints;
using ParserTests.Helpers;

namespace ParserTests.Algorithms;

public sealed class ShuntingYardAlgorithmTest
{
    [Test]
    public void ShouldHandle_SingleWhere_Value()
    {
        var result = BooleanExpressionParser.Parse("erp->customers.name = 'test'");

        result.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "test");
    }
    
    [Test]
    public void ShouldHandle_SingleWhere_Attribute()
    {
        var result = BooleanExpressionParser.Parse("erp->customers.name = erp->orders.ordered_by");

        result.ShouldBeBinaryOperator("=", 
            new AttributeSpecifier("erp", "customers", "name"),
            new AttributeSpecifier("erp", "orders", "ordered_by"));
    }
    
    [Test]
    public void ShouldHandle_AndWhere()
    {
        var result = BooleanExpressionParser.Parse("erp->customers.name = 'test' AND erp->customers.number = 1234");
        
        result.ShouldBeBinaryOperator("AND");
        var op = (BinaryOperatorWhereNode)result;
        op.Left.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "test");
        op.Right.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "number"), "1234");
    }
    
    [Test]
    public void ShouldHandle_OrWhere()
    {
        var result = BooleanExpressionParser.Parse("erp->customers.name = 'test' OR erp->customers.number = 1234");
        
        result.ShouldBeBinaryOperator("OR");
        var op = (BinaryOperatorWhereNode)result;
        op.Left.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "test");
        op.Right.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "number"), "1234");
    }
    
    [Test]
    public void ShouldHandle_AndOrWhere()
    {
        var result = BooleanExpressionParser.Parse("erp->customers.name = erp->orders.ordered_by AND erp->customers.name = 'test' OR erp->customers.number = 1234");
        
        result.ShouldBeBinaryOperator("OR");
        var op = (BinaryOperatorWhereNode)result;
        op.Left.ShouldBeBinaryOperator("AND");
        var leftOp = (BinaryOperatorWhereNode)op.Left;
        leftOp.Left.ShouldBeBinaryOperator("=", 
            new AttributeSpecifier("erp", "customers", "name"), 
            new AttributeSpecifier("erp", "orders", "ordered_by"));
        leftOp.Right.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "test");
        
        op.Right.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "number"), "1234");
    }
    
    [Test]
    public void ShouldHandle_AndOrParenthesesWhere()
    {
        var result = BooleanExpressionParser.Parse("erp->customers.name = erp->orders.ordered_by AND (erp->customers.name = 'test' OR erp->customers.number = 1234)");
        
        result.ShouldBeBinaryOperator("AND");
        var op = (BinaryOperatorWhereNode)result;
        op.Left.ShouldBeBinaryOperator("=", 
            new AttributeSpecifier("erp", "customers", "name"), 
            new AttributeSpecifier("erp", "orders", "ordered_by"));
        
        op.Right.ShouldBeBinaryOperator("OR");
        var rightOp = (BinaryOperatorWhereNode)op.Right;
        rightOp.Left.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "test");
        
        rightOp.Right.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "number"), "1234");
    }
}
