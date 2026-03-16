using AlliumSativum.Compiler;
using AlliumSativum.QueryExecutor.Performance.Histogram;
using AlliumSativum.Shared.Costs;
using Microsoft.AspNetCore.Mvc;

namespace AlliumSativum.QueryServer.Controllers;

[Controller]
[Route("[controller]")]
public sealed class BenchmarkController
{
    private readonly QueryCompiler _compiler;
    private readonly QueryExecutor.QueryExecutor _queryExecutor;
    private readonly ICostModel _costModel;
    private readonly ILogger<BenchmarkController> _logger;
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

    [HttpPost("winning-plan-accuracy")]
    public async Task<dynamic> GetWinningPlanAccuracy([FromBody] List<string> queries)
    {
        var results = new List<BenchmarkPredictCorrectPlanResult>();
        
        foreach (var query in queries)
        {
            var plans = await _compiler.CompileNoPruningAsync(query);

            List<BenchmarkPredictCorrectPlanSingleResult> benchmarks = [];
            var index = 0;
            foreach (var plan in plans)
            {
                var _ = await _queryExecutor.ExecuteAsync(plan.RootOperator);
                plan.RootOperator.StripExecutionResultData();
                benchmarks.Add(new BenchmarkPredictCorrectPlanSingleResult
                {
                    ActualCost = _costModel.TotalCost(plan.RootOperator, fromActualCost: true),
                    EstimatedCost = plan.TotalCost,
                    WasWinningPlan = plan == plans.MinBy(x => x.TotalCost)
                });
                
                _logger.LogInformation("Handled plan no. {PlanNumber} (of {TotalCount} for query: {Query}.", index++, plans.Count, query);
            }

            var orderedResults = benchmarks.OrderBy(x => x.ActualCost).ToList();

            var winningPlan = orderedResults.First(x => x.WasWinningPlan);
            results.Add(new BenchmarkPredictCorrectPlanResult
            {
                ChoseWinningPlan = orderedResults[0].WasWinningPlan,
                WinningPlanLocation = orderedResults.FindIndex(x => x.WasWinningPlan) + 1,
                PlanCount = orderedResults.Count,
                Query = query,
                OffByMs = winningPlan.ActualCost - orderedResults[0].ActualCost,
                OffByPercent = (winningPlan.ActualCost - orderedResults[0].ActualCost) /
                               winningPlan.ActualCost *
                               100,
                DurationAscendingMs = orderedResults.Select(x => x.ActualCost).ToList()
            });
            
            _logger.LogInformation("Finished benchmarking query: {Query}.", query);
        }

        return new
        {
            ChoseWinningPlanCount = results.Count(x => x.ChoseWinningPlan),
            AverageWinningPlanLocation = results.Average(x => x.WinningPlanLocation),
            OfAveragePlanCount = results.Average(x => x.PlanCount),
            Results = results
        };
    }

    [HttpPost("reconstructed-histograms")]
    public Task<ReconstructionSimilarityResult> GetReconstructedHistograms([FromBody] List<string> queries)
    {
        return _reconstructionDistanceService.ReconstructionSimilarityAsync(queries);
    }
    
    [HttpGet("reconstructed-histograms/{dataSourceId:guid}")]
    public Task<ReconstructionSimilarityResult> GetReconstructedHistograms([FromRoute] Guid dataSourceId, [FromQuery] List<string> ignore)
    {
        return _reconstructionDistanceService.ReconstructionSimilarityOfDatasourceAsync(dataSourceId, ignore);
    }
}

public class BenchmarkPredictCorrectPlanResult
{
    public bool ChoseWinningPlan { get; set; }
    public int WinningPlanLocation { get; set; }
    public int PlanCount { get; set; }
    public double OffByMs { get; set; }
    public double OffByPercent { get; set; }
    public List<double> DurationAscendingMs { get; set; }
    public string Query { get; set; } = string.Empty;
}

public class BenchmarkPredictCorrectPlanSingleResult
{
    public double EstimatedCost { get; set; }
    public double ActualCost { get; set; }
    public bool WasWinningPlan { get; set; }
}