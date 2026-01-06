using AlliumSativum.Parser;
using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;
using FluentAssertions;
using ParserTests.Helpers;

namespace ParserTests;

// more tests are run in BooleanExpressionParserTest
public sealed class WhereSelectQueries
{
    private static QueryParser _parser = new QueryParser();
    
    [Test]
    public void ShouldParse_SingleWhere()
    {
        var result = _parser.Parse("SELECT erp->customers.name FROM erp->customers WHERE erp->customers.name='John Doe'");
        result.Should().NotBeNull();

        result.From.ShouldBeTable("erp", "customers");
        result.Select.ShouldContainAttribute("erp", "customers", "name");
        result.Where.Should().NotBeNull();
        result.Where.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "John Doe");
    }
    
    [Test]
    public void ShouldParse_MultipleWhereBlocks()
    {
        var result = _parser.Parse("SELECT erp->customers.name FROM erp->customers WHERE erp->customers.name='John Doe' WHERE erp->customers.age>25");
        result.Should().NotBeNull();

        result.From.ShouldBeTable("erp", "customers");
        result.Select.ShouldContainAttribute("erp", "customers", "name");
        result.Where.Should().NotBeNull();
        result.Where.ShouldBeBinaryOperator("AND");

        var whereQuery = (BinaryOperatorExpressionNode)result.Where;
        whereQuery.Left.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "John Doe");
        whereQuery.Right.ShouldBeBinaryOperator(">", new AttributeSpecifier("erp", "customers", "age"), "25");
    }
}
