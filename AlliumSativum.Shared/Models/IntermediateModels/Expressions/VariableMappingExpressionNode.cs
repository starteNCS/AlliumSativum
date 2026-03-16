using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.IntermediateModels.Expressions;

public class VariableMappingExpressionNode : ExpressionNode
{
    public required VariableMappingSpecifier VariableMapping { get; set; }

    public override string ToString()
    {
        return
            $"[{VariableMapping.VariableName}{AsSqlParameters.Attribute.TableSeparator}{VariableMapping.AttributeName}]";
    }

    public override string ToSqlQueryString()
    {
        return $"{VariableMapping.VariableName}.{VariableMapping.AttributeName}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not VariableMappingExpressionNode other) return false;

        return other.VariableMapping.Equals(VariableMapping);
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