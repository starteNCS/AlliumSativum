using AlliumSativum.Parser;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Token;
using FluentAssertions;
using ParserTests.Helpers;

namespace ParserTests.Parser;

// more tests on the join expressions are run in BooleanExpressionParserTest
public sealed class JoinSelectQueries
{
    private static readonly Tokenizer Tokenizer = new Tokenizer();
    private static readonly TokenQueryParser TokenQueryParser = new TokenQueryParser();
    
    [Test]
    public void ShouldParse_SingleJoin()
    {
        var query = "SELECT erp->customers.name FROM erp->customers INNER JOIN erp->customers c ON c.name='John Doe'";
        var tokens = Tokenizer.Tokenize(query);
        var result = TokenQueryParser.Parse(tokens);

        result.Should().NotBeNull();
        result.VariableMappings.Should().Contain(mapping => mapping.Alias == "c" && mapping.Table.TableName == "customers" && mapping.Table.DataSourceName == "erp");
        
        result.Join.Count.Should().Be(1);
        var join = result.Join.Single();
        join.Inner.ShouldBeTable("erp", "customers");
        join.JoinType.Should().Be(JoinType.Inner);
        join.Expression.ShouldBeBinaryOperator("=", new VariableMappingSpecifier("c", "name"), "John Doe");
    }
    
    [Test]
    public void ShouldParse_MultiJoin()
    {
        var query = """
                    SELECT erp->customers.name 
                    FROM erp->customers 
                    INNER JOIN erp->customers c ON c.name='John Doe'
                    LEFT JOIN erp->orders o ON o.number = c.number
                    """;
        var tokens = Tokenizer.Tokenize(query);
        var result = TokenQueryParser.Parse(tokens);

        result.Should().NotBeNull();
        result.VariableMappings.Should().Contain(mapping => mapping.Alias == "c" && mapping.Table.TableName == "customers" && mapping.Table.DataSourceName == "erp");
        result.VariableMappings.Should().Contain(mapping => mapping.Alias == "o" && mapping.Table.TableName == "orders" && mapping.Table.DataSourceName == "erp");
        
        result.Join.Count.Should().Be(2);
        var firstJoin = result.Join[0];
        firstJoin.Inner.ShouldBeTable("erp", "customers");
        firstJoin.JoinType.Should().Be(JoinType.Inner);
        firstJoin.Expression.ShouldBeBinaryOperator("=", new VariableMappingSpecifier("c", "name"), "John Doe");
        
        var secondJoin = result.Join[1];
        secondJoin.Inner.ShouldBeTable("erp", "orders");
        secondJoin.JoinType.Should().Be(JoinType.Left);
        secondJoin.Expression.ShouldBeBinaryOperator("=", new VariableMappingSpecifier("o", "number"), new VariableMappingSpecifier("c", "number"));
    }
}
