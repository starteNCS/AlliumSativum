using AlliumSativum.Parser;
using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;
using AlliumSativum.Token;
using FluentAssertions;
using ParserTests.Helpers;

namespace ParserTests;

// more tests are run in BooleanExpressionParserTest
public sealed class WhereSelectQueries
{
    
    [Test]
    public void ShouldParse_SingleWhere()
    {
        var query = "SELECT erp->customers.name FROM erp->customers WHERE erp->customers.name='John Doe'";
        var tokens = Tokenizer.Tokenize(query);
        var result = TokenQueryParser.Parse(tokens);
        result.Should().NotBeNull();

        result.From.ShouldBeTable("erp", "customers");
        result.Select.ShouldContainAttributeSpecifier("erp", "customers", "name");
        result.Where.Should().NotBeNull();
        result.Where.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "John Doe");
    }
    
    [Test]
    public void ShouldParse_MultipleWhereBlocks()
    {
        var query =
            "SELECT erp->customers.name FROM erp->customers WHERE erp->customers.name='John Doe' WHERE erp->customers.age>25";
        var tokens = Tokenizer.Tokenize(query);
        var result = TokenQueryParser.Parse(tokens);
        result.Should().NotBeNull();

        result.From.ShouldBeTable("erp", "customers");
        result.Select.ShouldContainAttributeSpecifier("erp", "customers", "name");
        result.Where.Should().NotBeNull();
        result.Where.ShouldBeBinaryOperator("AND");

        var whereQuery = (BinaryOperatorExpressionNode)result.Where;
        whereQuery.Left.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "John Doe");
        whereQuery.Right.ShouldBeBinaryOperator(">", new AttributeSpecifier("erp", "customers", "age"), "25");
    }
}
