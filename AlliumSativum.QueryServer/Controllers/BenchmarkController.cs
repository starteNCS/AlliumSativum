using AlliumSativum.Compiler;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Models.ExecutionPlan;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AlliumSativum.QueryServer.Controllers;

[Controller]
[Route("[controller]")]
public sealed class BenchmarkController
{
    private readonly QueryCompiler _compiler;
    private readonly QueryExecutor.QueryExecutor _queryExecutor;
    private readonly ICostModel _costModel;

    public BenchmarkController(
        QueryCompiler compiler,
        QueryExecutor.QueryExecutor queryExecutor,
        ICostModel costModel)
    {
        _compiler = compiler;
        _queryExecutor = queryExecutor;
        _costModel = costModel;
    }

    [HttpPost("winning-plan-accuracy")]
    public async Task<dynamic> GetWinningPlanAccuracy([FromBody] List<string> queries)
    {
        var queryTasks = queries.Select(async query =>
        {
            var plans = await _compiler.CompileNoPruningAsync(query);

            var tasks = plans
                .Select(async plan =>
                {
                    await _queryExecutor.ExecuteAsync(plan.RootOperator);
                    return new BenchmarkPredictCorrectPlanSingleResult
                    {
                        ActualCost = _costModel.TotalCost(plan.RootOperator, fromActualCost: true),
                        EstimatedCost = plan.TotalCost,
                        WasWinningPlan = plan == plans.MinBy(x => x.TotalCost)
                    };
                });
            var results = await Task.WhenAll(tasks);

            var orderedResults = results.OrderBy(x => x.ActualCost).ToList();

            var winningPlan = orderedResults.First(x => x.WasWinningPlan);
            return new BenchmarkPredictCorrectPlanResult
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
            };
        });

        var finalResults = await Task.WhenAll(queryTasks);

        return new
        {
            ChoseWinningPlanCount = finalResults.Count(x => x.ChoseWinningPlan),
            AverageWinningPlanLocation = finalResults.Average(x => x.WinningPlanLocation),
            OfAveragePlanCount = finalResults.Average(x => x.PlanCount),
            Results = finalResults
        };
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