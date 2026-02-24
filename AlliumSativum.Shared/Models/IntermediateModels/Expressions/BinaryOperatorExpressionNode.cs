namespace AlliumSativum.Shared.Models.IntermediateModels.Expressions;

public class BinaryOperatorExpressionNode : ExpressionNode
{
    public string Operation { get; set; } = string.Empty;
    public required ExpressionNode Left { get; set; }
    public required ExpressionNode Right { get; set; }
    public override string ToString() => $"({Left} {Operation} {Right})";
    public override string ToSqlQueryString() => $"({Left.ToSqlQueryString()} {Operation} {Right.ToSqlQueryString()})";
    
    public override bool Equals(object? obj)
    {
        if (obj is not BinaryOperatorExpressionNode other)
        {
            return false;
        }
        
        return other.Operation == Operation && other.Left.Equals(Left) && other.Right.Equals(Right);
    }

    public override object? ResolveValue(Dictionary<string, object> row)
    {
        return null;
    }

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

    public bool EvaluateEquals(Dictionary<string, object> row)
    {
        var left = Left.ResolveValue(row);
        var right = Right.ResolveValue(row);
        return Equals(left, right);
    }

    public int EvaluateComparison(Dictionary<string, object> row)
    {
        var left = Left.ResolveValue(row);
        var right = Right.ResolveValue(row);

        if (left is IComparable lc && right is IComparable rc)
            return lc.CompareTo(rc);

        throw new InvalidOperationException($"Cannot compare {left?.GetType()} and {right?.GetType()}");
    }
}