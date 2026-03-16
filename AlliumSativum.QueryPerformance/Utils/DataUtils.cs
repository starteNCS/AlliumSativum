using System.Text.Json;
using AlliumSativum.Shared.Models.ExecutionPlan;

namespace AlliumSativum.QueryServer.Utils;

public sealed class DataUtils
{
    private readonly QueryExecutor.QueryExecutor _queryExecutor;

    public DataUtils(QueryExecutor.QueryExecutor queryExecutor)
    {
        _queryExecutor = queryExecutor;
    }
    
    public async Task<List<double>> LoadDataAsync(QueryExecutionPlan plan)
    {
        var result = await _queryExecutor.ExecuteAsync(plan.RootOperator);
        var parsed = result
            .Select(x => (JsonElement?) x.Single().Value)
            .Where(x => x is null || x.Value.ValueKind == JsonValueKind.Number)
            .Select(x =>
            {
                if (x is null || !x.Value.TryGetDouble(out var value))
                {
                    return double.NaN;
                }

                return value;
            })
            .ToList();
        return parsed;
    }
}
