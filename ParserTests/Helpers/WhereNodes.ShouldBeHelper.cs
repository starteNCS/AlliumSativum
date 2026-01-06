using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;
using FluentAssertions;

namespace ParserTests.Helpers;

public static partial class ShouldBeHelper
{
    extension(IWhereNode node)
    {
        public void ShouldBeBinaryOperator(string @operator,
            AttributeSpecifier left, string right)
        {
            node.Should().BeOfType<BinaryOperatorWhereNode>();
            var binaryOperator = (BinaryOperatorWhereNode)node;
        
            binaryOperator.Operation.Should().Be(@operator);
        
            binaryOperator.Left.Should().BeOfType<FullySpecifiedColumnWhereNode>();
            var leftAnd = (FullySpecifiedColumnWhereNode)binaryOperator.Left;
            leftAnd.Attribute.ShouldBeAttribute(left.DataSourceName, left.TableName, left.AttributeName);
        
            binaryOperator.Right.Should().BeOfType<ValueWhereNode>();
            var rightAnd = (ValueWhereNode)binaryOperator.Right;
            rightAnd.Value.Should().Be(right);
        }

        public void ShouldBeBinaryOperator(string @operator,
            AttributeSpecifier left, AttributeSpecifier right)
        {
            node.Should().BeOfType<BinaryOperatorWhereNode>();
            var binaryOperator = (BinaryOperatorWhereNode)node;
        
            binaryOperator.Operation.Should().Be(@operator);
        
            binaryOperator.Left.Should().BeOfType<FullySpecifiedColumnWhereNode>();
            var leftAnd = (FullySpecifiedColumnWhereNode)binaryOperator.Left;
            leftAnd.Attribute.ShouldBeAttribute(left.DataSourceName, left.TableName, left.AttributeName);
        
            binaryOperator.Right.Should().BeOfType<FullySpecifiedColumnWhereNode>();
            var rightAnd = (FullySpecifiedColumnWhereNode)binaryOperator.Right;
            rightAnd.Attribute.ShouldBeAttribute(right.DataSourceName, right.TableName, right.AttributeName);
        }
        
        public void ShouldBeBinaryOperator(string @operator)
        {
            node.Should().BeOfType<BinaryOperatorWhereNode>();
            var binaryOperator = (BinaryOperatorWhereNode)node;
        
            binaryOperator.Operation.Should().Be(@operator);
            binaryOperator.Left.Should().BeOfType<BinaryOperatorWhereNode>();
            binaryOperator.Right.Should().BeOfType<BinaryOperatorWhereNode>();
        }
    }
}
