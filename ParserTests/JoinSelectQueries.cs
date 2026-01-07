using AlliumSativum.Parser;
using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;
using FluentAssertions;
using ParserTests.Helpers;

namespace ParserTests;

// more tests on the join expressions are run in BooleanExpressionParserTest
public sealed class JoinSelectQueries
{
    private static QueryParser _parser = new QueryParser();
    
    [Test]
    public void ShouldParse_SingleJoin()
    {
        var result = _parser.Parse("SELECT erp->customers.name FROM erp->customers INNER JOIN erp->customers c ON c.name='John Doe'");

        ;
    }
}
