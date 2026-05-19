using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class JoinPlanOperator : PlanOperator
{
    public JoinPlanOperator(PlanOperator left, ExpressionNode expression, PlanOperator right)
    {
        Left = left;
        Expression = expression;
        Right = right;

        base.Children.AddRange(left, right);
    }

    public PlanOperator Left { get; }
    public ExpressionNode Expression { get; }
    public PlanOperator Right { get; }

    // override to avoid some outer class to add more children
    public new IReadOnlyList<PlanOperator> Children => base.Children;

    protected override string GetNodeInfo()
    {
        return $"JOIN: {Expression} INCOMPLETE! MISSING JOIN ALGORITHM INFO";
    }

    protected override string GetNodeInfoHtml()
    {
        return
            $"{HtmlClasses.Bold(HtmlClasses.Colored("JOIN", "green"))}: {Expression} {HtmlClasses.Bold(HtmlClasses.Colored("INCOMPLETE! MISSING JOIN ALGORITHM INFO", "red"))}";
    }

    protected override double GetActualSelectivityInfo()
    {
        return (double)ExecutionData.ActualCardinality /
               (Left.ExecutionData.ActualCardinality * Right.ExecutionData.ActualCardinality);
    }
    
    public override string ToJoinPlanString()
    {
        return "Invalid, no join type selected";
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Left, Expression, Right);
    }
    
    public override bool Equals(object? other)
    {
        if (other is not JoinPlanOperator join) return false;

        return join.Left.Equals(Left) && join.Right.Equals(Right) && join.Expression.Equals(Expression);
    }
}