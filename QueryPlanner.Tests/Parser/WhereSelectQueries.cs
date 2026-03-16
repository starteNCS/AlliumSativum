using AlliumSativum.Parser;
using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Token;
using FluentAssertions;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.Parser;

// more tests are run in BooleanExpressionParserTest
public sealed class WhereSelectQueries
{
    private static readonly Tokenizer Tokenizer = new();
    private static readonly TokenQueryParser TokenQueryParser = new();

    #region NegativeTests

    [Test]
    public void ShouldNotParse_MultipleWhereBlocks()
    {
        var query =
            "SELECT erp->customers.name FROM erp->customers WHERE erp->customers.name='John Doe' WHERE erp->customers.age>25";
        var tokens = Tokenizer.Tokenize(query);
        Action action = () => TokenQueryParser.Parse(tokens);

        action.ShouldThrowParseException("",
            $"Only one {AsSqlKeywords.WHERE} statement is allowed. Please combine them using AND");
    }

    #endregion

    #region PositiveTests

    [Test]
    public void ShouldParse_SingleWhere()
    {
        var query = "SELECT erp->customers.name FROM erp->customers WHERE erp->customers.name='John Doe'";
        var tokens = Tokenizer.Tokenize(query);
        var result = TokenQueryParser.Parse(tokens);
        result.Should().NotBeNull();

        result.From.Should().NotBeNull();
        result.From.ShouldBeTable("erp", "customers");
        result.Select.ShouldContainAttributeSpecifier("erp", "customers", "name");
        result.Where.Should().NotBeNull();
        result.Where.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers", "name"), "John Doe");
    }

    [Test]
    public void ShouldParse_SingleWhere_VariableMapping()
    {
        var query = "SELECT erp->customers.name FROM erp->customers c WHERE c.name='John Doe'";
        var tokens = Tokenizer.Tokenize(query);
        var result = TokenQueryParser.Parse(tokens);
        result.Should().NotBeNull();

        result.From.Should().NotBeNull();
        result.From.ShouldBeTable("erp", "customers");
        result.Select.ShouldContainAttributeSpecifier("erp", "customers", "name");
        result.Where.Should().NotBeNull();
        result.Where.ShouldBeBinaryOperator("=", new VariableMappingSpecifier("c", "name"), "John Doe");
    }

    #endregion
}