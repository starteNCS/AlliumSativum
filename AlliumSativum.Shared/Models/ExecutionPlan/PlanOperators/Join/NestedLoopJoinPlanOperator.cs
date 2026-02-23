using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;

public sealed class NestedLoopJoinPlanOperator : JoinPlanOperator
{
    public NestedLoopJoinPlanOperator(PlanOperator left, ExpressionNode expression, PlanOperator right) : base(left, expression, right)
    {
    }
    
    public static NestedLoopJoinPlanOperator FromJoinPop(JoinPlanOperator joinPop)
    {
        return new NestedLoopJoinPlanOperator(joinPop.Left, joinPop.Expression, joinPop.Right);
    }

    protected override string GetNodeInfo() => $"JOIN [NESTED LOOP]: {Expression}";
    protected override string GetNodeInfoHtml() =>
        $"{HtmlClasses.Bold(HtmlClasses.Colored("JOIN", color: "green"))} [{HtmlClasses.Italic(HtmlClasses.Colored("NESTED LOOP", color: "gray"))}]: {Expression}";
}
