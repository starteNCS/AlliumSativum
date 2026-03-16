using AlliumSativum.Optimize;
using AlliumSativum.Parser;
using AlliumSativum.Semantic;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Token;

namespace AlliumSativum.Compiler;

public class QueryCompiler
{
    private readonly Optimizer _optimizer;
    private readonly TokenQueryParser _parser;
    private readonly SemanticTransformer _semanticTransformer;
    private readonly Tokenizer _tokenizer;

    public QueryCompiler(Tokenizer tokenizer, TokenQueryParser parser, SemanticTransformer semanticTransformer,
        Optimizer optimizer)
    {
        _tokenizer = tokenizer;
        _parser = parser;
        _semanticTransformer = semanticTransformer;
        _optimizer = optimizer;
    }

    public async Task<QueryExecutionPlan> CompileAsync(string query)
    {
        var tokens = _tokenizer.Tokenize(query);
        var selectModel = _parser.Parse(tokens);
        if (selectModel is null) throw new AsSqlException("Failed to parse query.");
        _semanticTransformer.Transform(selectModel);
        // TODO: semantic checker (check attributes etc.)
        var executionPlan = await _optimizer.OptimizeAsync(selectModel);

        return executionPlan.Single();
    }

    public async Task<List<QueryExecutionPlan>> CompileNoPruningAsync(string query)
    {
        var tokens = _tokenizer.Tokenize(query);
        var selectModel = _parser.Parse(tokens);
        if (selectModel is null) throw new AsSqlException("Failed to parse query.");
        _semanticTransformer.Transform(selectModel);
        // TODO: semantic checker (check attributes etc.)
        var executionPlan = await _optimizer.OptimizeAsync(selectModel, false);

        return executionPlan;
    }
}