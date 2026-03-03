using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.IntermediateModels.Expressions;

public class FullySpecifiedColumnExpressionNode : ExpressionNode
{
    public required AttributeSpecifier Attribute { get; set; }
    public override string ToString() => $"[{Attribute.DataSourceName}{AsSqlParameters.Attribute.DataSourceSeparator}{Attribute.TableName}{AsSqlParameters.Attribute.TableSeparator}{Attribute.AttributeName}]";
    
    // currently discards the data source attribute
    public override string ToSqlQueryString() => $"{Attribute.TableName}.{Attribute.AttributeName}";
    
    public override bool Equals(object? obj)
    {
        if (obj is not FullySpecifiedColumnExpressionNode other)
        {
            return false;
        }
        
        return other.Attribute.Equals(Attribute);
    }

    public override object? ResolveValue(Dictionary<string, object> row)
    {
        return row.GetValueOrDefault(Attribute.ToDictKey())?.ToString();
    }

    public override bool EvaluatePredicate(Dictionary<string, object> row)
    {
        return row.TryGetValue(Attribute.ToDictKey(), out var val) && val is not null;
    }
}