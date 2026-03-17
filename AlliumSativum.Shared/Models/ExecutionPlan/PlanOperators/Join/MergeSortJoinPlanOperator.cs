using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;

public sealed class MergeSortJoinPlanOperator : JoinPlanOperator
{
    public MergeSortJoinPlanOperator(PlanOperator left, ExpressionNode expression, PlanOperator right) : base(left,
        expression, right)
    {
    }

    public static MergeSortJoinPlanOperator FromJoinPop(JoinPlanOperator joinPop)
    {
        return new MergeSortJoinPlanOperator(joinPop.Left, joinPop.Expression, joinPop.Right)
        {
            DistributionData = joinPop.DistributionData
        };
    }

    protected override string GetNodeInfo()
    {
        return $"JOIN [MERGE SORT]: {Expression}";
    }

    protected override string GetNodeInfoHtml()
    {
        return
            $"{HtmlClasses.Bold(HtmlClasses.Colored("JOIN", "green"))} [{HtmlClasses.Italic(HtmlClasses.Colored("MERGE SORT", "gray"))}]: {Expression}";
    }

    public override string ToJoinPlanString()
    {
        throw new NotImplementedException();
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Left, Expression, Right);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not MergeSortJoinPlanOperator other)
        {
            return false;
        }

        return other.Left.Equals(Left) && other.Right.Equals(Right) && other.Expression.Equals(Expression);
    }
}