using System.Text.Json;
using AlliumSativum.Shared.Models.ExecutionPlan;

namespace AlliumSativum.QueryPerformance.Utils;

public sealed class DataUtils
{
    private readonly QueryExecutor.QueryExecutor _queryExecutor;

    public DataUtils(QueryExecutor.QueryExecutor queryExecutor)
    {
        _queryExecutor = queryExecutor;
    }

    /// <summary>
    /// Executes the given query execution plan and extracts the resulting data as a list of doubles.
    /// </summary>
    /// <remarks>
    /// If a value cannot be parsed as a double, it is represented as NaN in the resulting list.
    /// </remarks>
    /// <param name="plan">The plan to execute</param>
    /// <returns>List of parsed doubles</returns>
    public async Task<List<double>> LoadDataAsync(QueryExecutionPlan plan)
    {
        var result = await _queryExecutor.ExecuteAsync(plan.RootOperator);
        var parsed = result
            .Select(x => (JsonElement?)x.Single().Value)
            .Where(x => x is null || x.Value.ValueKind == JsonValueKind.Number)
            .Select(x =>
            {
                if (x is null || !x.Value.TryGetDouble(out var value)) return double.NaN;

                return value;
            })
            .ToList();
        return parsed;
    }
}