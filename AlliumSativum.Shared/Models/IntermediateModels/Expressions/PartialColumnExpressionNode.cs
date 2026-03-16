namespace AlliumSativum.Shared.Models.IntermediateModels.Expressions;

public class PartialColumnExpressionNode : ExpressionNode
{
    public string Name { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"[{Name}]";
    }

    public override string ToSqlQueryString()
    {
        return "invalid";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not PartialColumnExpressionNode other) return false;

        return other.Name.Equals(Name);
    }

    public override object? ResolveValue(Dictionary<string, object> row)
    {
        return null;
    }

    public override bool EvaluatePredicate(Dictionary<string, object> row)
    {
        return false;
    }
}