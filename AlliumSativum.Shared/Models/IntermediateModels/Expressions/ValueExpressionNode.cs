namespace AlliumSativum.Shared.Models.IntermediateModels.Expressions;

public class ValueExpressionNode : ExpressionNode
{
    public ValueExpressionType Type { get; init; }
    public string Value { get; init; } = string.Empty;
    public override string ToString() => Type switch
    {
        ValueExpressionType.String => $"'{Value}'",
        ValueExpressionType.Numeric => Value,
        _ => "false"
    };
    public override string ToSqlQueryString() => ToString();
    
    public override bool Equals(object? obj)
    {
        if (obj is not ValueExpressionNode other)
        {
            return false;
        }
        
        return other.Value.Equals(Value) &&  other.Type.Equals(Type);
    }

    public override object? ResolveValue(Dictionary<string, object> row)
    {
        return Type switch
        {
            ValueExpressionType.String => Value,
            ValueExpressionType.Numeric => double.TryParse(Value, out var num) ? num : null,
            _ => null
        };
    }

    public override bool EvaluatePredicate(Dictionary<string, object> row)
    {
        return false;
    }

    public enum ValueExpressionType
    {
        String = 0,
        Numeric = 1
    }
}