using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;
using FluentAssertions;

namespace ParserTests.Helpers;

public static partial class ShouldBeHelper
{
    extension(IExpressionNode node)
    {
        public void ShouldBeBinaryOperator(string @operator,
            AttributeSpecifier left, string right)
        {
            node.Should().BeOfType<BinaryOperatorExpressionNode>();
            var binaryOperator = (BinaryOperatorExpressionNode)node;
        
            binaryOperator.Operation.Should().Be(@operator);
        
            binaryOperator.Left.Should().BeOfType<FullySpecifiedColumnExpressionNode>();
            var leftAnd = (FullySpecifiedColumnExpressionNode)binaryOperator.Left;
            leftAnd.Attribute.ShouldBeAttribute(left.DataSourceName, left.TableName, left.AttributeName);
        
            binaryOperator.Right.Should().BeOfType<ValueExpressionNode>();
            var rightAnd = (ValueExpressionNode)binaryOperator.Right;
            rightAnd.Value.Should().Be(right);
        }
        
        public void ShouldBeBinaryOperator(string @operator,
            VariableMappingSpecifier left, string right)
        {
            node.Should().BeOfType<BinaryOperatorExpressionNode>();
            var binaryOperator = (BinaryOperatorExpressionNode)node;
        
            binaryOperator.Operation.Should().Be(@operator);
        
            binaryOperator.Left.Should().BeOfType<VariableMappingExpressionNode>();
            var leftAnd = (VariableMappingExpressionNode)binaryOperator.Left;
            leftAnd.VariableMapping.ShouldBeVariableMapping(left.VariableName, left.AttributeName);
        
            binaryOperator.Right.Should().BeOfType<ValueExpressionNode>();
            var rightAnd = (ValueExpressionNode)binaryOperator.Right;
            rightAnd.Value.Should().Be(right);
        }

        public void ShouldBeBinaryOperator(string @operator,
            AttributeSpecifier left, AttributeSpecifier right)
        {
            node.Should().BeOfType<BinaryOperatorExpressionNode>();
            var binaryOperator = (BinaryOperatorExpressionNode)node;
        
            binaryOperator.Operation.Should().Be(@operator);
        
            binaryOperator.Left.Should().BeOfType<FullySpecifiedColumnExpressionNode>();
            var leftAnd = (FullySpecifiedColumnExpressionNode)binaryOperator.Left;
            leftAnd.Attribute.ShouldBeAttribute(left.DataSourceName, left.TableName, left.AttributeName);
        
            binaryOperator.Right.Should().BeOfType<FullySpecifiedColumnExpressionNode>();
            var rightAnd = (FullySpecifiedColumnExpressionNode)binaryOperator.Right;
            rightAnd.Attribute.ShouldBeAttribute(right.DataSourceName, right.TableName, right.AttributeName);
        }
        
        public void ShouldBeBinaryOperator(string @operator)
        {
            node.Should().BeOfType<BinaryOperatorExpressionNode>();
            var binaryOperator = (BinaryOperatorExpressionNode)node;
        
            binaryOperator.Operation.Should().Be(@operator);
            binaryOperator.Left.Should().BeOfType<BinaryOperatorExpressionNode>();
            binaryOperator.Right.Should().BeOfType<BinaryOperatorExpressionNode>();
        }
    }
}
