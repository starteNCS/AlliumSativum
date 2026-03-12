using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;

public sealed class HashJoinPlanOperator : JoinPlanOperator
{
    public HashJoinPlanOperator(PlanOperator left, ExpressionNode expression, PlanOperator right) : base(left, expression, right)
    {
    }
    
    public static HashJoinPlanOperator FromJoinPop(JoinPlanOperator joinPop)
    {
        return new HashJoinPlanOperator(joinPop.Left, joinPop.Expression, joinPop.Right)
        {
            DistributionData = joinPop.DistributionData,
        };
    }

    protected override string GetNodeInfo() => $"JOIN [HASH]: {Expression}";
    protected override string GetNodeInfoHtml() =>
        $"{HtmlClasses.Bold(HtmlClasses.Colored("JOIN", color: "green"))} [{HtmlClasses.Italic(HtmlClasses.Colored("HASH", color: "gray"))}]: {Expression}";
}
