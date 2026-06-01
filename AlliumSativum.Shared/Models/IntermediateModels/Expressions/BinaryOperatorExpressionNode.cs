namespace AlliumSativum.Shared.Models.IntermediateModels.Expressions;

/// <summary>
///     Expression node representing a binary operator (e.g., AND, OR, =, >, <, etc.)
/// </summary>
public class BinaryOperatorExpressionNode : ExpressionNode
{
    public string Operation { get; set; } = string.Empty;
    public required ExpressionNode Left { get; set; }
    public required ExpressionNode Right { get; set; }

    public override string ToString()
    {
        return $"({Left} {Operation} {Right})";
    }

    public override string ToSqlQueryString()
    {
        return $"({Left.ToSqlQueryString()} {Operation} {Right.ToSqlQueryString()})";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BinaryOperatorExpressionNode other) return false;

        return other.Operation == Operation && other.Left.Equals(Left) && other.Right.Equals(Right);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Operation, Left, Right);
    }

    /// <inheritdoc />
    public override object? ResolveValue(Dictionary<string, object> row)
    {
        return null;
    }

    /// <inheritdoc />
    public override bool EvaluatePredicate(Dictionary<string, object> row)
    {
        return Operation switch
        {
            "=" => EvaluateEquals(row),
            "!=" => !EvaluateEquals(row),
            ">" => EvaluateComparison(row) > 0,
            "<" => EvaluateComparison(row) < 0,
            ">=" => EvaluateComparison(row) >= 0,
            "<=" => EvaluateComparison(row) <= 0,
            "AND" => Left.EvaluatePredicate(row) && Right.EvaluatePredicate(row),
            "OR" => Left.EvaluatePredicate(row) || Right.EvaluatePredicate(row),
            _ => throw new NotSupportedException($"Unsupported operation: {Operation}")
        };
    }

    private bool EvaluateEquals(Dictionary<string, object> row)
    {
        var left = Left.ResolveValue(row);
        var right = Right.ResolveValue(row);
        return Equals(left?.ToString(), right?.ToString());
    }

    private int EvaluateComparison(Dictionary<string, object> row)
    {
        if (!double.TryParse(Left.ResolveValue(row)?.ToString(), out var leftNum) ||
            !double.TryParse(Right.ResolveValue(row)?.ToString(), out var rightNum)) return -1;

        return leftNum.CompareTo(rightNum);
    }
}