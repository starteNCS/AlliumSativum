using AlliumSativum.Compiler;
using AlliumSativum.Interfaces;
using AlliumSativum.Optimize;
using AlliumSativum.Parser;
using AlliumSativum.Semantic;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Token;
using NSubstitute;

namespace QueryPlanner.Tests;

public class CompilerTest
{
    private readonly ITokenizer _tokenizer = Substitute.For<ITokenizer>();
    private readonly ITokenQueryParser _tokenQueryParser = Substitute.For<ITokenQueryParser>();
    private readonly ISemanticTransformer _semanticTransformer = Substitute.For<ISemanticTransformer>();
    private readonly IOptimizer _optimizer = Substitute.For<IOptimizer>();
    
    [Test]
    // this test is not meant to be a unit test, but rather an integration test to ensure that the compiler calls all the necessary components in the correct order.
    // the actual logic of the components is not tested here, but rather in their respective unit tests
    public async Task ShouldCompile()
    {
        QueryCompiler Compiler = new(_tokenizer, _tokenQueryParser, _semanticTransformer, _optimizer);
        
        _tokenizer.Tokenize(Arg.Any<string>()).Returns(new Stack<string>());
        _tokenQueryParser.Parse(Arg.Any<Stack<string>>()).Returns(new SelectBaseModel());
        // SemanticTransformer operates in-place, so we don't need to return anything from it.
        _optimizer.OptimizeAsync(Arg.Any<SelectBaseModel>()).Returns(Task.FromResult(new List<QueryExecutionPlan>()
        {
            null!
        }));
        
        
        await Compiler.CompileAsync("SELECT t.attr FROM source->table t");
        
        Received.InOrder(async () =>
        {
            _tokenizer.Tokenize(Arg.Any<string>());
            _tokenQueryParser.Parse(Arg.Any<Stack<string>>());
            _semanticTransformer.Transform(Arg.Any<SelectBaseModel>());
            await _optimizer.OptimizeAsync(Arg.Any<SelectBaseModel>());
        });
    }
}
