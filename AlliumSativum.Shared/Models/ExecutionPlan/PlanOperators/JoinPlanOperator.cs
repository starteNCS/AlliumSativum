using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class JoinPlanOperator : PlanOperator
{
    public PlanOperator Left { get; }
    public ExpressionNode Expression { get; }
    public PlanOperator Right { get;  }

    public JoinPlanOperator(PlanOperator left, ExpressionNode expression, PlanOperator right)
    {
        Left = left;
        Expression = expression;
        Right = right;
        
        base.Children.AddRange(left, right);
    }
    
    // override to avoid some outer class to add more children
    public new IReadOnlyList<PlanOperator> Children => base.Children;
    
    protected override string GetNodeInfo() => $"JOIN: {Expression} INCOMPLETE! MISSING JOIN ALGORITHM INFO";
    protected override string GetNodeInfoHtml() => $"{HtmlClasses.Bold(HtmlClasses.Colored("JOIN", color: "green"))}: {Expression} {HtmlClasses.Bold(HtmlClasses.Colored("INCOMPLETE! MISSING JOIN ALGORITHM INFO", color: "red"))}";
    
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