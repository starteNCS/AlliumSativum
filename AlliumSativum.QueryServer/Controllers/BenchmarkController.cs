using System.Collections.Concurrent;
using System.Diagnostics;
using AlliumSativum.Compiler;
using AlliumSativum.QueryPerformance.Histogram;
using AlliumSativum.QueryPerformance.Selectivity;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Models;
using AlliumSativum.Shared.Models.ExecutionPlan;
using Microsoft.AspNetCore.Mvc;

namespace AlliumSativum.QueryServer.Controllers;

[Controller]
[Route("[controller]")]
public sealed class BenchmarkController
{
    private readonly QueryCompiler _compiler;
    private readonly ICostModel _costModel;
    private readonly ILogger<BenchmarkController> _logger;
    private readonly QueryExecutor.QueryExecutor _queryExecutor;
    private readonly ReconstructionDistanceService _reconstructionDistanceService;

    public BenchmarkController(
        QueryCompiler compiler,
        QueryExecutor.QueryExecutor queryExecutor,
        ICostModel costModel,
        ILogger<BenchmarkController> logger,
        ReconstructionDistanceService reconstructionDistanceService)
    {
        _compiler = compiler;
        _queryExecutor = queryExecutor;
        _costModel = costModel;
        _logger = logger;
        _reconstructionDistanceService = reconstructionDistanceService;
    }

    /// <summary>
    /// Calcualtes the proximity of the winning plan to all enumerated contestant plans for a given set of queries
    /// </summary>
    /// <param name="queries">All queries to test</param>
    /// <returns>For each query, all run times for all QExP's and if it was the winning plan</returns>
    [HttpPost("winning-plan-accuracy")]
    public async Task<dynamic> GetWinningPlanAccuracy([FromBody] Dictionary<string, string> queries)
    {
        var results = new List<BenchmarkPredictCorrectPlanResult>();

        foreach (var query in queries)
        {
            var plans = await _compiler.CompileNoPruningAsync(query.Value);
            var generatedWinningPlan = await _compiler.CompileAsync(query.Value);

            ConcurrentBag<BenchmarkPredictCorrectPlanSingleResult> benchmarks = [];
            var index = 0;
            var page = 1;
            const int pageSize = 1;
            var chunked = plans.Chunk(pageSize).ToList();
            foreach (var chunk in chunked)
            {
                _logger.LogInformation("Begin batch no. {Page} (of {TotalPages}) for query: {JoinPlan}.", page++, chunked.Count, query);
                var tasks = chunk.Select(async plan =>
                {
                    await _queryExecutor.ExecuteAsync(plan.RootOperator);

                    plan.RootOperator.StripExecutionResultData();
                    benchmarks.Add(new BenchmarkPredictCorrectPlanSingleResult
                    {
                        ActualCost = _costModel.TotalCost(plan.RootOperator, true),
                        EstimatedCost = plan.TotalCost,
                        JoinPlan = plan.RootOperator.ToJoinPlanString(),
                        WasWinningPlan = plan.RootOperator.IsEquivalentTo(generatedWinningPlan.RootOperator)
                    });

                    _logger.LogInformation("Handled plan no. {PlanNumber} (of {TotalCount}) for query: {Query}.", index++,
                        plans.Count, plan.RootOperator.ToJoinPlanString());
                }).ToList();
                await Task.WhenAll(tasks);
            }

            var orderedResults = benchmarks.OrderBy(x => x.ActualCost).ToList();

            var winningPlan = orderedResults.First(x => x.WasWinningPlan);
            results.Add(new BenchmarkPredictCorrectPlanResult
            {
                ChoseWinningPlan = orderedResults[0].WasWinningPlan,
                WinningPlanLocation = orderedResults.FindIndex(x => x.WasWinningPlan) + 1,
                PlanCount = orderedResults.Count,
                Query = query.Value,
                QueryShort = query.Key,
                OffByMs = winningPlan.ActualCost - orderedResults[0].ActualCost,
                OffByPercent = (winningPlan.ActualCost - orderedResults[0].ActualCost) /
                               winningPlan.ActualCost *
                               100,
                OrderedData = orderedResults.ToList()
            });

            _logger.LogInformation("Finished benchmarking query: {Query}.", query);
        }

        return results;
    }

    /// <summary>
    /// Returns the Earth Mover's Distance between the original and reconstructed histograms for a given set of queries
    /// as well as the individual distances for each query.
    /// </summary>
    /// <param name="queries">Queries to test</param>
    /// <returns>The nEMD</returns>
    [HttpPost("reconstructed-histograms")]
    public Task<ReconstructionSimilarityResult> GetReconstructedHistograms([FromBody] List<string> queries)
    {
        return _reconstructionDistanceService.ReconstructionSimilarityAsync(queries);
    }
    
    /// <summary>
    /// Returns the Earth Mover's Distance between the original and reconstructed histograms for all attributes of a given data source
    /// </summary>
    /// <param name="dataSourceId">Id of the data source</param>
    /// <param name="ignore">Which attributes to ignore</param>
    /// <returns>The nEMD</returns>
    [HttpGet("reconstructed-histograms/{dataSourceId:guid}")]
    public Task<ReconstructionSimilarityResult> GetReconstructedHistograms([FromRoute] Guid dataSourceId,
        [FromQuery] string? ignore)
    {
        return _reconstructionDistanceService.ReconstructionSimilarityOfDatasourceAsync(dataSourceId, ignore?.Split(',').ToList() ?? []);
    }
    
    /// <summary>
    /// Returns the Earth Mover's Distance between the original and reconstructed histograms for all attributes in the catalog
    /// </summary>
    /// <param name="ignore">Which attributes to ignore</param>
    /// <returns>The nEMD</returns>
    [HttpGet("reconstructed-histograms/all")]
    public Task<ReconstructionSimilarityResult> GetReconstructedHistograms([FromQuery] string? ignore)
    {
        return _reconstructionDistanceService.ReconstructionSimilarityOfAllDatasourcesAsync(ignore?.Split(',').ToList() ?? []);
    }
    
    /// <summary>
    /// Gets all exact timings for all steps in the compiling and execution
    /// </summary>
    /// <param name="queries">Queries to test</param>
    /// <returns>The timings</returns>
    [HttpPost("timings")]
    public async Task<object> GetCompileTiming([FromBody] List<string> queries)
    {
        var (plan, timingResult) = await _compiler.TimedCompileAsync(queries.Single());
        
        var stopwatch = Stopwatch.StartNew();
        await _queryExecutor.ExecuteAsync(plan.RootOperator);
        timingResult.Execute = stopwatch.Elapsed;

        return timingResult.ToMilliSeconds();
    }
    
    /// <summary>
    /// For a given set of queries, executes the winning plan and evaluates the predicted selectivity vs. the actual selectivity for each operator in the execution tree.
    /// </summary>
    /// <param name="queries">Queries to test</param>
    /// <param name="excludeOnes">Whether to include selectivities, that are set to "1"</param>
    /// <returns>The selectivities per stage</returns>
    [HttpPost("selectivity-per-stage")]
    public async Task<object> GetSelectivityPerStage([FromBody] Dictionary<string, string> queries, [FromQuery] bool excludeOnes = false)
    {
        var results = new List<object>();
        
        foreach (var query in queries)
        {
            var compiled = await _compiler.CompileAsync(query.Value);
            await _queryExecutor.ExecuteAsync(compiled.RootOperator);
            
            results.Add(new
            {
                Query = query.Key,
                SelectivityPerStage = SelectivityEvaluationService.EvaluateExecutedTree(compiled.RootOperator, excludeOnes),
            });
        }
        
        return results;
    }
}

public class BenchmarkPredictCorrectPlanResult
{
    public bool ChoseWinningPlan { get; set; }
    public int WinningPlanLocation { get; set; }
    public int PlanCount { get; set; }
    public double OffByMs { get; set; }
    public double OffByPercent { get; set; }
    public List<BenchmarkPredictCorrectPlanSingleResult> OrderedData { get; set; } = [];
    public string Query { get; set; } = string.Empty;
    public string QueryShort { get; set; } = string.Empty;
}

public class BenchmarkPredictCorrectPlanSingleResult
{
    public double EstimatedCost { get; set; }
    public double ActualCost { get; set; }
    public bool WasWinningPlan { get; set; }
    public string JoinPlan { get; set; }
}