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
        var min = pop.Left.ExpectedCardinality <= pop.Right.ExpectedCardinality ? pop.Left : pop.Right;
        var max = pop.Left.ExpectedCardinality > pop.Right.ExpectedCardinality ? pop.Left : pop.Right;
        var minData = min.ExecutionData.Data;
        var maxData = max.ExecutionData.Data;

        if (minData.Count == 0 || maxData.Count == 0)
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

        var minJoinKey = joinKeys.FirstOrDefault(aSpec => minData[0].TryGetValue(aSpec.ToDictKey(), out _));
        if (minJoinKey is null)
            throw new AsSQLExecuteException("Did not find a valid join key for the hash join operator. Join keys: " +
                                            string.Join(", ", joinKeys.Select(k => k.ToDictKey())) + ". Data keys: " +
                                            string.Join(", ", minData[0].Keys));
        var maxJoinKey = joinKeys.FirstOrDefault(aSpec => maxData[0].TryGetValue(aSpec.ToDictKey(), out _));
        if (maxJoinKey is null)
            throw new AsSQLExecuteException("Did not find a valid join key for the hash join operator. Join keys: " +
                                            string.Join(", ", joinKeys.Select(k => k.ToDictKey())) + ". Data keys: " +
                                            string.Join(", ", maxData[0].Keys));

        var result = new List<Dictionary<string, object>>();

        var hashTable = new Dictionary<object, List<Dictionary<string, object>>>();

        foreach (var item in minData)
        {
            if (!item.TryGetValue(minJoinKey.ToDictKey(), out var key) || key == null) continue;

            if (!hashTable.TryGetValue(GetJsonContent(key), out var list))
            {
                list = new List<Dictionary<string, object>>();
                hashTable[GetJsonContent(key)] = list;
            }

            list.Add(item);
        }

        foreach (var item in maxData)
        {
            if (!item.TryGetValue(maxJoinKey.ToDictKey(), out var probeKey) || probeKey == null) continue;

            if (!hashTable.TryGetValue(GetJsonContent(probeKey), out var matches)) continue;

            foreach (var match in matches)
            {
                var merged = Merge(item, match);
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

    private object GetJsonContent(object obj)
    {
        if (obj is not JsonElement json) return obj;

        if (json.ValueKind == JsonValueKind.Number) return json.GetDouble();

        return json.ToString();
    }
}