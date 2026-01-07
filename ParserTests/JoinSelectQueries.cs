using AlliumSativum.Parser;
using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;
using AlliumSativum.Token;
using FluentAssertions;
using ParserTests.Helpers;

namespace ParserTests;

// more tests on the join expressions are run in BooleanExpressionParserTest
public sealed class JoinSelectQueries
{
    
    [Test]
    public void ShouldParse_SingleJoin()
    {
        var query = "SELECT erp->customers.name FROM erp->customers INNER JOIN erp->customers c ON c.name='John Doe'";
        var tokens = Tokenizer.Tokenize(query);
        var result = TokenQueryParser.Parse(tokens);

        ;
    }
}
