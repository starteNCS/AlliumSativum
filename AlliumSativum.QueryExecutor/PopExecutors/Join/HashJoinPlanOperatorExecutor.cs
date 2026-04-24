using System.Diagnostics;
using System.Text.Json;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;

namespace AlliumSativum.QueryExecutor.PopExecutors.Join;

public sealed class HashJoinPlanOperatorExecutor : IPlanOperatorExecutor<HashJoinPlanOperator>
{
    public Task<PlanOperator> ExecuteAsync(HashJoinPlanOperator pop)
    {
        var stopwatch = Stopwatch.StartNew();
        var left = pop.Left.ExecutionData.Data;
        var right = pop.Right.ExecutionData.Data;
        
        
        if (left.Count == 0 || right.Count == 0)
        {
            stopwatch.Stop();
            pop.ExecutionData = new PlanOperatorExecutionData
            {
                Data = [],
                Materialized = true,
                ActualCardinality = 0,
                ActualCost = stopwatch.Elapsed.TotalMilliseconds
            };

            return Task.FromResult<PlanOperator>(pop);
        }

        var joinKeys = pop.Expression.GetAttributesOfExpression();

        var leftJoinKey = joinKeys.FirstOrDefault(aSpec => left[0].TryGetValue(aSpec.ToDictKey(), out _));
        if (leftJoinKey is null)
            throw new AsSqlExecuteException("Did not find a valid join key for the hash join operator. Join keys: " +
                                            string.Join(", ", joinKeys.Select(k => k.ToDictKey())) + ". Data keys: " +
                                            string.Join(", ", left[0].Keys));
        var rightJoinKey = joinKeys.FirstOrDefault(aSpec => right[0].TryGetValue(aSpec.ToDictKey(), out _));
        if (rightJoinKey is null)
            throw new AsSqlExecuteException("Did not find a valid join key for the hash join operator. Join keys: " +
                                            string.Join(", ", joinKeys.Select(k => k.ToDictKey())) + ". Data keys: " +
                                            string.Join(", ", right[0].Keys));

        var result = new List<Dictionary<string, object>>();

        var hashTable = new Dictionary<object, List<Dictionary<string, object>>>();

        foreach (var item in left)
        {
            if (!item.TryGetValue(leftJoinKey.ToDictKey(), out var key) || key == null) continue;

            var jsonContent = GetJsonContent(key);
            if (!hashTable.TryGetValue(jsonContent, out var list))
            {
                list = new List<Dictionary<string, object>>();
                hashTable[jsonContent] = list;
            }

            list.Add(item);
        }

        foreach (var item in right)
        {
            if (!item.TryGetValue(rightJoinKey.ToDictKey(), out var probeKey) || probeKey == null) continue;

            if (!hashTable.TryGetValue(GetJsonContent(probeKey), out var matches)) continue;

            foreach (var match in matches)
            {
                var merged = Merge(match, item);
                if (pop.Expression.EvaluatePredicate(merged)) result.Add(merged);
            }
        }

        stopwatch.Stop();

        pop.ExecutionData = new PlanOperatorExecutionData
        {
            Data = result,
            Materialized = true,
            ActualCardinality = result.Count,
            ActualCost = stopwatch.Elapsed.TotalMilliseconds
        };

        return Task.FromResult<PlanOperator>(pop);
    }

    private static Dictionary<string, object> Merge(Dictionary<string, object> left, Dictionary<string, object> right)
    {
        var merged = new Dictionary<string, object>(left);
        foreach (var kvp in right) merged[kvp.Key] = kvp.Value;

        return merged;
    }

    private static object GetJsonContent(object obj)
    {
        if (obj is not JsonElement json)
        {
            return obj;
        }

        if (json.ValueKind == JsonValueKind.Number)
        {
            return json.GetDouble();
        }

        return json.ToString();
    }
}