using AlliumSativum;
using AlliumSativum.Parser;
using AlliumSativum.Parser.Constants;
using AlliumSativum.Token;
using FluentAssertions;
using ParserTests.Helpers;

namespace ParserTests;

public sealed class SimpleSelectQueries
{
    #region PositiveTests
    [Test]
    public void ShouldParse_SingleAttribute()
    {
        var tokens = Tokenizer.Tokenize("SELECT erp->customers.name FROM erp->customers");
        var result = TokenQueryParser.Parse(tokens);
        result.Should().NotBeNull();

        result.From.ShouldBeTable("erp", "customers");
        result.Select.ShouldContainAttribute("erp", "customers", "name");
    }

    [Test]
    public void ShouldParse_MultipleAttributes()
    {
        var tokens = Tokenizer.Tokenize("SELECT erp->customers.name, erp->customers.customer_number FROM erp->customers");
        var result = TokenQueryParser.Parse(tokens);
        result.Should().NotBeNull();

        result.From.ShouldBeTable("erp", "customers");
        result.Select.ShouldContainAttribute("erp", "customers", "name");
        result.Select.ShouldContainAttribute("erp", "customers", "customer_number");
    }
    #endregion

    #region NegativeTests
    [Test]
    public void ShouldNotParse_Select_InvalidDataSourceSeparator()
    {
        var tokens = Tokenizer.Tokenize("SELECT erp.customers.name FROM erp->customers");
        Action action = () => TokenQueryParser.Parse(tokens);
        action.ShouldThrowParseException("erp.", $"expected datasource separator, got '.'");
    }
    
    [Test]
    public void ShouldNotParse_Select_InvalidTableNameSeparator()
    {
        var tokens = Tokenizer.Tokenize("SELECT erp->customersname FROM erp->customers");
        Action action = () => TokenQueryParser.Parse(tokens);
        action.ShouldThrowParseException("erp->customersnameFROM", $"expected table name separator, got 'FROM'");
    }
    
    [Test]
    public void ShouldNotParse_From_InvalidDataSourceSeparator()
    {
        var tokens = Tokenizer.Tokenize("SELECT erp->customers.name FROM erp.customers");
        Action action = () => TokenQueryParser.Parse(tokens);
        action.ShouldThrowParseException("erp.", $"expected datasource separator, got '.'");
    }
    #endregion
}
