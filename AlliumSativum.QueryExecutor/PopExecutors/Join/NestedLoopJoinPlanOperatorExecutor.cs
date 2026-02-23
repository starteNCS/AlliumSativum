using System.Diagnostics;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.QueryExecutor.PopExecutors.Join;

public sealed class NestedLoopJoinPlanOperatorExecutor : IPlanOperatorExecutor<NestedLoopJoinPlanOperator>
{
    public Task<PlanOperator> ExecuteAsync(NestedLoopJoinPlanOperator pop)
    {
        var stopwatch = Stopwatch.StartNew();
        var left = pop.Left.ExecutionData.Data;
        var right = pop.Right.ExecutionData.Data;

        var result = new List<Dictionary<string, object>>();
        foreach (var leftRow in left)
        {
            foreach (var rightRow in right)
            {
                var merged = Merge(leftRow, rightRow);
                if (EvaluatePredicate(pop.Expression, merged))
                {
                    result.Add(merged);
                }
            }
        }

        stopwatch.Stop();

        pop.ExecutionData = new PlanOperatorExecutionData
        {
            Data = result,
            Materialized = true,
            ActualCardinality = result.Count,
            ActualCost = stopwatch.ElapsedMilliseconds,
        };

        return Task.FromResult<PlanOperator>(pop);
    }

    private static Dictionary<string, object> Merge(Dictionary<string, object> left, Dictionary<string, object> right)
    {
        var merged = new Dictionary<string, object>(left);
        foreach (var kvp in right)
        {
            merged[kvp.Key] = kvp.Value;
        }
        
        return merged;
    }
    
    private static bool EvaluatePredicate(ExpressionNode node, Dictionary<string, object> row) => node switch
    {
        ValueExpressionNode { Type: ValueExpressionNode.ValueExpressionType.Numeric } v => v.Value != "0",
        FullySpecifiedColumnExpressionNode col => row.TryGetValue(col.Attribute.AttributeName, out var val) && val is not null,
        BinaryOperatorExpressionNode binary => binary.Operation switch
        {
            "="  => EvaluateEquals(binary, row),
            "!=" => !EvaluateEquals(binary, row),
            ">"  => EvaluateComparison(binary, row) > 0,
            "<"  => EvaluateComparison(binary, row) < 0,
            ">=" => EvaluateComparison(binary, row) >= 0,
            "<=" => EvaluateComparison(binary, row) <= 0,
            "AND" => EvaluatePredicate(binary.Left, row) && EvaluatePredicate(binary.Right, row),
            "OR"  => EvaluatePredicate(binary.Left, row) || EvaluatePredicate(binary.Right, row),
            _ => throw new NotSupportedException($"Unsupported operation: {binary.Operation}")
        },
        _ => throw new NotSupportedException($"Unsupported expression node: {node.GetType().Name}")
    };

    private static bool EvaluateEquals(BinaryOperatorExpressionNode node, Dictionary<string, object> row)
    {
        var left = ResolveValue(node.Left, row);
        var right = ResolveValue(node.Right, row);
        return Equals(left, right);
    }

    private static int EvaluateComparison(BinaryOperatorExpressionNode node, Dictionary<string, object> row)
    {
        var left = ResolveValue(node.Left, row);
        var right = ResolveValue(node.Right, row);

        if (left is IComparable lc && right is IComparable rc)
            return lc.CompareTo(rc);

        throw new InvalidOperationException($"Cannot compare {left?.GetType()} and {right?.GetType()}");
    }

    private static object? ResolveValue(ExpressionNode node, Dictionary<string, object> row) => node switch
    {
        FullySpecifiedColumnExpressionNode col => row.GetValueOrDefault(col.Attribute.AttributeName)?.ToString(),
        ValueExpressionNode { Type: ValueExpressionNode.ValueExpressionType.Numeric } v => decimal.Parse(v.Value),
        ValueExpressionNode { Type: ValueExpressionNode.ValueExpressionType.String } v => v.Value,
        _ => throw new NotSupportedException($"Cannot resolve value from {node.GetType().Name}")
    };
}
