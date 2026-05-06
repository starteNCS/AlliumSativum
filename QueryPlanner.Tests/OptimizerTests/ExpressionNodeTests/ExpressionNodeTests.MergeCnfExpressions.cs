using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using FluentAssertions;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.OptimizerTests.ExpressionNodeTests;

public sealed class MergeCnfExpressionsTests
{
    private readonly ExpressionNodeTestFixture _fixture = new();

    [Test]
    public void Should_Return_Null_For_Null_Input()
    {
        var result = _fixture.ExpressionNodeOptimizer.MergeCnfExpressions(null, null);
        result.Should().BeNull();
    }
    
    [Test]
    public void Should_Return_Other_If_One_Is_Null()
    {
        var node = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.String, "test");
        var result1 = _fixture.ExpressionNodeOptimizer.MergeCnfExpressions(node, null);
        var result2 = _fixture.ExpressionNodeOptimizer.MergeCnfExpressions(null, node);
        
        result1.Should().BeExpressionNode(node);
        result2.Should().BeExpressionNode(node);
    }

    [Test]
    public void Should_Merge_Expressions()
    {
        var node1 = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.String, "test");
        var node2 = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.String, "test2");
        
        var result = _fixture.ExpressionNodeOptimizer.MergeCnfExpressions(node1, node2);
        
        result.Should().BeExpressionNode(new BinaryOperatorExpressionNode
        {
            Left = node1,
            Operation = "AND",
            Right = node2
        });
    }
}
