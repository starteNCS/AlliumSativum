using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class JoinPlanOperator : PlanOperator
{
    public PlanOperator Left { get; }
    public IExpressionNode Expression { get; }
    public PlanOperator Right { get;  }

    public JoinPlanOperator(PlanOperator left, IExpressionNode expression, PlanOperator right)
    {
        Left = left;
        Expression = expression;
        Right = right;
        
        base.Children.AddRange(left, right);
    }
    
    // override to avoid some outer class to add more children
    public new IReadOnlyList<PlanOperator> Children => base.Children;
    
    protected override string GetNodeInfo() => $"{GetBaseNodeInto()} INNER JOIN: {Expression}";
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Left, Expression, Right);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not JoinPlanOperator other)
        {
            return false;
        }
        
        return other.Left.Equals(Left) && other.Right.Equals(Right) && other.Expression.Equals(Expression);
    }
}