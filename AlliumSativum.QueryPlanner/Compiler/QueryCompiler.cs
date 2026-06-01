using System.Diagnostics;
using AlliumSativum.Interfaces;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models;
using AlliumSativum.Shared.Models.ExecutionPlan;

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

    /// <summary>
    ///     Default compilation settings. Turns the query into one optimal execution plan
    /// </summary>
    /// <param name="query">Query string in AsSQL</param>
    /// <returns>Optimal Query Execution Plan</returns>
    /// <exception cref="AsSqlException">Query could not be parsed</exception>
    public async Task<QueryExecutionPlan> CompileAsync(string query)
    {
        var tokens = _tokenizer.Tokenize(query);
        var selectModel = _parser.Parse(tokens);
        if (selectModel is null) throw new AsSqlException("Failed to parse query.");
        _semanticTransformer.Transform(selectModel);
        var executionPlan = await _optimizer.OptimizeAsync(selectModel);

        return executionPlan.Single();
    }

    /// <summary>
    ///     Times all compilation steps in addition to compiling the query.
    ///     /// May be used for benchmarking purposes.
    /// </summary>
    /// <param name="query">Query in AsSQL</param>
    /// <returns>
    ///     - plan: Optimal Query Execution Plan
    ///     - timingResult: Time taken for each compilation step (tokenization, parsing, semantic transformation, optimization)
    /// </returns>
    /// <exception cref="AsSqlException">Query could not be parsed</exception>
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


    /// <summary>
    ///     Compiles the query without pruning the join tree. All possible execution plans for a given query are enumerated.
    ///     May be used for benchmarking purposes.
    /// </summary>
    /// <param name="query">Query in AsSQL</param>
    /// <returns>List of all possible plans for this query</returns>
    /// <exception cref="AsSqlException">Query could not be parsed</exception>
    public async Task<List<QueryExecutionPlan>> CompileNoPruningAsync(string query)
    {
        var tokens = _tokenizer.Tokenize(query);
        var selectModel = _parser.Parse(tokens);
        if (selectModel is null) throw new AsSqlException("Failed to parse query.");
        _semanticTransformer.Transform(selectModel);
        var executionPlan = await _optimizer.OptimizeAsync(selectModel, false);

        return executionPlan;
    }
}