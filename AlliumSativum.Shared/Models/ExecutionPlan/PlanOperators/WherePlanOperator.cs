using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class WherePlanOperator : PlanOperator
{
    public ExpressionNode Expression { get; }

    public WherePlanOperator(ExpressionNode expression)
    {
        Expression = expression;
    }
    
    protected override string GetNodeInfo() => $"{GetBaseNodeInto()} FILTER: {Expression}";
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Expression);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not WherePlanOperator other)
        {
            return false;
        }
        
        return other.Expression.Equals(Expression);
    }
}