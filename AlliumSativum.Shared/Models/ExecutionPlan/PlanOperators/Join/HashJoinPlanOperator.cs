using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;

public sealed class HashJoinPlanOperator : JoinPlanOperator
{
    public HashJoinPlanOperator(PlanOperator left, ExpressionNode expression, PlanOperator right) : base(left,
        expression, right)
    {
    }

    protected override string GetNodeInfo()
    {
        return $"JOIN [HASH]: {Expression}";
    }

    protected override string GetNodeInfoHtml()
    {
        return
            $"{HtmlClasses.Bold(HtmlClasses.Colored("JOIN", "green"))} [{HtmlClasses.Italic(HtmlClasses.Colored("HASH", "gray"))}]: {Expression}";
    }

    public override string ToJoinPlanString()
    {
        return $"HASH JOIN ({Left.ToJoinPlanString()}, {Right.ToJoinPlanString()})";
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Left, Expression, Right);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not HashJoinPlanOperator other)
        {
            return false;
        }

        return other.Left.Equals(Left) && other.Right.Equals(Right) && other.Expression.Equals(Expression);
    }
    
    public override bool IsEquivalentTo(PlanOperator? other)
    {
        if (!base.IsEquivalentTo(other)) return false;
        return other is HashJoinPlanOperator otherJoin && Equals(otherJoin);
    }
}