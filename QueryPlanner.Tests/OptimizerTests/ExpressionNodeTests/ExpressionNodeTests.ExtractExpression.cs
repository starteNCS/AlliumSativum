using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;
using NSubstitute;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.OptimizerTests.ExpressionNodeTests;

public sealed class ExtractExpressionTests
{
    private readonly ExpressionNodeTestFixture _fixture = new();
    
    [Test]
    public void Should_Return_Null_For_Null_Input()
    {
        var result = _fixture.ExpressionNodeOptimizer.ExtractExpression(null, []);
        
        result.@base.Should().BeNull();
        result.split.Should().BeNull();
    }
    
    [Test]
    public void Should_Return_Base_Node_For_Table_Not_Found()
    {
        var node = "SELECT a.id FROM cs->algorithm a WHERE a.id = 10".ToSelectDto().Where!;
        
        var result = _fixture.ExpressionNodeOptimizer.ExtractExpression(node, [
            new TableSpecifier("cs", "experiment_run")
        ]);
        
        result.@base.Should().BeExpressionNode(node);
        result.split.Should().BeNull();
    }
    
    [Test]
    public void Should_Return_Split_Node_When_Only_Table_In_Expr()
    {
        var node = "SELECT a.id FROM cs->algorithm a WHERE a.id = 10 AND a.id = 20".ToSelectDto().Where!;
        
        var result = _fixture.ExpressionNodeOptimizer.ExtractExpression(node, [
            new TableSpecifier("cs", "algorithm")
        ]);
        
        result.@base.Should().BeNull();
        result.split.Should().BeExpressionNode(node);
    }
    
    [Test]
    public void Should_Return_Split_When_Mixed_Expression()
    {
        var node = """
                   SELECT a.id 
                   FROM cs->algorithm a 
                    INNER JOIN cs->experiment_run er ON er.algorithm_id = a.id
                   WHERE a.id = 10 AND er.peak_memory_mb > 150000
                   """.ToSelectDto().Where!;
        
        var result = _fixture.ExpressionNodeOptimizer.ExtractExpression(node, [
            new TableSpecifier("cs", "algorithm")
        ]);
        
        result.@base.Should().BeExpressionNode(new BinaryOperatorExpressionNode
        {
            Left = FullySpecifiedColumnExpressionNode.FromValues("cs", "experiment_run", "peak_memory_mb"),
            Operation = ">",
            Right = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.Numeric, "150000")
        });
        result.split.Should().BeExpressionNode(new BinaryOperatorExpressionNode
        {
            Left = FullySpecifiedColumnExpressionNode.FromValues("cs", "algorithm", "id"),
            Operation = "=",
            Right = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.Numeric, "10")
        });
    }
}
