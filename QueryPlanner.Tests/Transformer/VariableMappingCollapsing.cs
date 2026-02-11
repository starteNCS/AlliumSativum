using AlliumSativum.Parser;
using AlliumSativum.Semantic;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Token;
using FluentAssertions;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.Transformer;

public sealed class VariableMappingCollapsing
{
    private static readonly Tokenizer Tokenizer = new Tokenizer();
    private static readonly TokenQueryParser TokenQueryParser = new TokenQueryParser();
    private static readonly SemanticTransformer SemanticTransformer = new SemanticTransformer();
    
    #region Select
    [Test]
    public void ShouldCollapse_Select()
    {
        const string query = "SELECT c.name FROM erp->customers c";
        var tokens = Tokenizer.Tokenize(query);
        var parsed = TokenQueryParser.Parse(tokens);

        parsed.Should().NotBeNull();
        parsed.Select.Should().HaveCount(1);
        parsed.VariableMappings.Should().Contain(vm => vm.Alias == "c" && vm.Table.TableName == "customers" && vm.Table.DataSourceName == "erp");
        parsed.Select.ShouldContainVariableMappingSpecifier("c", "name");
        
        SemanticTransformer.Transform(parsed);
        parsed.Select.Should().HaveCount(1);
        parsed.Select.ShouldContainAttributeSpecifier("erp", "customers", "name");
    }
    
    [Test]
    public void ShouldNotCollapse_Select_ThrowVariableNotFound()
    {
        const string query = "SELECT v.name FROM erp->customers c";
        var tokens = Tokenizer.Tokenize(query);
        var parsed = TokenQueryParser.Parse(tokens);
        
        Action action = () => SemanticTransformer.Transform(parsed);
        action.ShouldThrowSemanticException($"variable mapping not found: 'v'");
    }
    #endregion
    
    #region Where
    [Test]
    public void ShouldCollapse_Where()
    {
        const string query = "SELECT c.name FROM erp->customers c WHERE c.name = 'John Doe'";
        var tokens = Tokenizer.Tokenize(query);
        var parsed = TokenQueryParser.Parse(tokens);

        parsed.Should().NotBeNull();
        parsed.Where.Should().NotBeNull();
        parsed.Where.ShouldBeBinaryOperator("=", new VariableMappingSpecifier("c",  "name"), "John Doe");
        
        SemanticTransformer.Transform(parsed);
        parsed.Where.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "customers",  "name"), "John Doe");
    }
    
    [Test]
    public void ShouldNotCollapse_Where_ThrowVariableNotFound()
    {
        const string query = "SELECT c.name FROM erp->customers c WHERE v.name = 'John Doe'";
        var tokens = Tokenizer.Tokenize(query);
        var parsed = TokenQueryParser.Parse(tokens);
        
        Action action = () => SemanticTransformer.Transform(parsed);
        action.ShouldThrowSemanticException($"variable mapping not found: 'v'");
    }
    #endregion
    
    #region Where
    [Test]
    public void ShouldCollapse_Join()
    {
        const string query = "SELECT c.name FROM erp->customers c INNER JOIN erp->orders o ON o.name = 'Apple'";
        var tokens = Tokenizer.Tokenize(query);
        var parsed = TokenQueryParser.Parse(tokens);

        parsed.Should().NotBeNull();
        parsed.Join.Should().HaveCount(1);
        parsed.Join.Single().Expression.ShouldBeBinaryOperator("=", new VariableMappingSpecifier("o",  "name"), "Apple");
        
        SemanticTransformer.Transform(parsed);
        parsed.Join.Single().Expression.ShouldBeBinaryOperator("=", new AttributeSpecifier("erp", "orders",  "name"), "Apple");
    }
    
    [Test]
    public void ShouldNotCollapse_Join_ThrowVariableNotFound()
    {
        const string query = "SELECT c.name FROM erp->customers c INNER JOIN erp->orders o ON v.name = 'Apple'";
        var tokens = Tokenizer.Tokenize(query);
        var parsed = TokenQueryParser.Parse(tokens);
        
        Action action = () => SemanticTransformer.Transform(parsed);
        action.ShouldThrowSemanticException($"variable mapping not found: 'v'");
    }
    #endregion
}
