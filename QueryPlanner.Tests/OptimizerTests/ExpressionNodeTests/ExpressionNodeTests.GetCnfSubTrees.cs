using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using FluentAssertions;

namespace QueryPlanner.Tests.OptimizerTests.ExpressionNodeTests;

public sealed class GetCnfSubTreesTests
{
    private readonly ExpressionNodeTestFixture _fixture = new();

    [Test]
    public void Should_Return_Null_For_Null_Input()
    {
        var result = _fixture.ExpressionNodeOptimizer.GetCnfSubTrees(null);
        result.Should().BeEmpty();
    }

    [Test]
    public void Should_Return_Same_Node_If_Not_Binary_Operator()
    {
        var node = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.String, "test");
        var result = _fixture.ExpressionNodeOptimizer.GetCnfSubTrees(node);
        result.Should().BeEquivalentTo(new List<ExpressionNode> { node });
    }

    [Test]
    public void Should_Return_Same_Node_If_Not_And_Operator()
    {
        var node = new BinaryOperatorExpressionNode
        {
            Left = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.String, "test"),
            Operation = "OR",
            Right = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.String, "test2")
        };

        var result = _fixture.ExpressionNodeOptimizer.GetCnfSubTrees(node);
        result.Should().BeEquivalentTo(new List<ExpressionNode> { node });
    }

    [Test]
    public void Should_Return_Sub_Trees_For_And_Operator()
    {
        var node1 = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.String, "test");
        var node2 = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.String, "test2");

        var node = new BinaryOperatorExpressionNode
        {
            Left = node1,
            Operation = "AND",
            Right = node2
        };

        var result = _fixture.ExpressionNodeOptimizer.GetCnfSubTrees(node);
        result.Should().BeEquivalentTo(new List<ExpressionNode> { node1, node2 });
    }
}