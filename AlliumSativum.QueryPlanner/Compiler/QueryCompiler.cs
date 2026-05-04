using System.Diagnostics;
using AlliumSativum.Interfaces;
using AlliumSativum.Optimize;
using AlliumSativum.Parser;
using AlliumSativum.Semantic;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Token;

namespace AlliumSativum.Compiler;

public class QueryCompiler
{
    private readonly IOptimizer _optimizer;
    private readonly ITokenQueryParser _parser;
    private readonly ISemanticTransformer _semanticTransformer;
    private readonly ITokenizer _tokenizer;

    public QueryCompiler(ITokenizer tokenizer, ITokenQueryParser parser, ISemanticTransformer semanticTransformer,
        IOptimizer optimizer)
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
    
    public async Task<(QueryExecutionPlan plan, AlliumSativumTimingResult timingResult)> TimedCompileAsync(string query)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new AlliumSativumTimingResult();
        
        var tokens = _tokenizer.Tokenize(query);
        result.Tokenize = stopwatch.Elapsed;
        
        stopwatch.Restart();
        var selectModel = _parser.Parse(tokens);
        result.Parse = stopwatch.Elapsed;
        
        if (selectModel is null) throw new AsSqlException("Failed to parse query.");
        
        stopwatch.Restart();
        _semanticTransformer.Transform(selectModel);
        result.SemanticTransform = stopwatch.Elapsed;
        
        stopwatch.Restart();
        var executionPlan = await _optimizer.OptimizeAsync(selectModel);
        result.Optimize = stopwatch.Elapsed;

        return (executionPlan.Single(), result);
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