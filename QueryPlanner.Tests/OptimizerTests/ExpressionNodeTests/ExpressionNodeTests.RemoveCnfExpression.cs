using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using FluentAssertions;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.OptimizerTests.ExpressionNodeTests;

public sealed class RemoveCnfExpressionTests
{
    private readonly ExpressionNodeTestFixture _fixture = new();
    
        [Test]
        public void Should_Return_Null_For_Null_Input()
        {
            var result = _fixture.ExpressionNodeOptimizer.RemoveCnfExpression(null, ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.Numeric, "1"));
            result.Should().BeNull();
        }

        [Test]
        public void Should_Remove_From_And()
        {
            var expr1 = new BinaryOperatorExpressionNode
            {
                Left = FullySpecifiedColumnExpressionNode.FromValues("cs", "algorithm", "id"),
                Operation = "=",
                Right = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.Numeric, "1")
            };
            var expr2 = new BinaryOperatorExpressionNode
            {
                Left = FullySpecifiedColumnExpressionNode.FromValues("cs", "algorithm", "name"),
                Operation = "=",
                Right = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.String, "test")
            };
            
            var node = new BinaryOperatorExpressionNode
            {
                Left = expr1,
                Operation = "AND",
                Right = expr2
            };
            
            var result = _fixture.ExpressionNodeOptimizer.RemoveCnfExpression(node, expr1);
            
            result.Should().BeExpressionNode(expr2);
        }
        
        [Test]
        public void Should_Remove_From_Deep_And()
        {
            var expr1 = new BinaryOperatorExpressionNode
            {
                Left = FullySpecifiedColumnExpressionNode.FromValues("cs", "algorithm", "id"),
                Operation = "=",
                Right = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.Numeric, "1")
            };
            var expr2 = new BinaryOperatorExpressionNode
            {
                Left = FullySpecifiedColumnExpressionNode.FromValues("cs", "algorithm", "name"),
                Operation = "=",
                Right = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.String, "test")
            };
            var remove = new BinaryOperatorExpressionNode
            {
                Left = FullySpecifiedColumnExpressionNode.FromValues("cs", "algorithm", "classification"),
                Operation = "=",
                Right = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.String, "test2")
            };
            
            var left = new BinaryOperatorExpressionNode
            {
                Left = expr1,
                Operation = "AND",
                Right = expr2
            };
            
            var node = new BinaryOperatorExpressionNode
            {
                Left = left,
                Operation = "AND",
                Right = remove
            };
            
            var result = _fixture.ExpressionNodeOptimizer.RemoveCnfExpression(node, remove);
            
            result.Should().BeExpressionNode(left);
        }
}
