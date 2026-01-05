using AlliumSativum;
using AlliumSativum.Constants;
using AlliumSativum.Exceptions;
using FluentAssertions;
using ParserTests.Helpers;

namespace ParserTests;

public sealed class SimpleSelectQueries
{
    private static QueryParser _parser = new QueryParser();

    #region PositiveTests
    [Test]
    public void ShouldParse_SingleAttribute()
    {
        var result = _parser.Parse("SELECT erp->customers.name FROM erp->customers");

        result.From.ShouldBeTable("erp", "customers");
        result.Select.ShouldContainAttribute("erp", "customers", "name");
    }

    [Test]
    public void ShouldParse_MultipleAttributes()
    {
        var result = _parser.Parse("SELECT erp->customers.name, erp->customers.customer_number FROM erp->customers");

        result.From.ShouldBeTable("erp", "customers");
        result.Select.ShouldContainAttribute("erp", "customers", "name");
        result.Select.ShouldContainAttribute("erp", "customers", "customer_number");
    }
    #endregion

    #region NegativeTests
    [Test]
    public void ShouldNotParse_InvalidDataSourceSeparator()
    {
        Action action = () => _parser.Parse("SELECT erp.customers.name FROM erp.customers");
        action.ShouldThrowParseException("erp.customers.name", $"Could not find datasource separator ({AsSQLParameters.Attribute.DataSourceSeparator})");
    }
    
    [Test]
    public void ShouldNotParse_InvalidTableNameSeparator()
    {
        Action action = () => _parser.Parse("SELECT erp->customersname FROM erp.customers");
        action.ShouldThrowParseException("erp->customersname", $"Invalid number of attribute separators (expected 2, found 1)");
    }
    #endregion
}
