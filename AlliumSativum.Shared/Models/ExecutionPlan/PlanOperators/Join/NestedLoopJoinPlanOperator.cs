using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;

public sealed class NestedLoopJoinPlanOperator : JoinPlanOperator
{
    public NestedLoopJoinPlanOperator(PlanOperator left, ExpressionNode expression, PlanOperator right) : base(left,
        expression, right)
    {
    }

    public static NestedLoopJoinPlanOperator FromJoinPop(JoinPlanOperator joinPop)
    {
        return new NestedLoopJoinPlanOperator(joinPop.Left, joinPop.Expression, joinPop.Right)
        {
            DistributionData = joinPop.DistributionData
        };
    }

    protected override string GetNodeInfo()
    {
        return $"JOIN [NESTED LOOP]: {Expression}";
    }

    protected override string GetNodeInfoHtml()
    {
        return
            $"{HtmlClasses.Bold(HtmlClasses.Colored("JOIN", "green"))} [{HtmlClasses.Italic(HtmlClasses.Colored("NESTED LOOP", "gray"))}]: {Expression}";
    }
}