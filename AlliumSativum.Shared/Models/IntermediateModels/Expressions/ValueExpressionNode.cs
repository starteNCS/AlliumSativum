namespace AlliumSativum.Shared.Models.IntermediateModels.Expressions;

/// <summary>
/// Node containing a plain value
/// </summary>
public class ValueExpressionNode : ExpressionNode
{
    public enum ValueExpressionType
    {
        String = 0,
        Numeric = 1
    }

    public ValueExpressionType Type { get; init; }
    public string Value { get; init; } = string.Empty;

    public override string ToString()
    {
        return Type switch
        {
            ValueExpressionType.String => $"'{Value}'",
            ValueExpressionType.Numeric => Value,
            _ => "false"
        };
    }

    public override string ToSqlQueryString()
    {
        return ToString();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ValueExpressionNode other) return false;

        return other.Value.Equals(Value) && other.Type.Equals(Type);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Value, Type);
    }

    /// <inheritdoc/>
    public override object? ResolveValue(Dictionary<string, object> row)
    {
        return Type switch
        {
            ValueExpressionType.String => Value,
            ValueExpressionType.Numeric => double.TryParse(Value, out var num) ? num : null,
            _ => null
        };
    }

    /// <inheritdoc/>
    public override bool EvaluatePredicate(Dictionary<string, object> row)
    {
        return false;
    }
    
    public static ValueExpressionNode FromValues(ValueExpressionType type, string value)
    {
        return new ValueExpressionNode
        {
            Type = type,
            Value = value
        };
    }
}