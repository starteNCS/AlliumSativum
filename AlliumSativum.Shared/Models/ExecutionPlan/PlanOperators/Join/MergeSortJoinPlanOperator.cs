using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;

public sealed class MergeSortJoinPlanOperator : JoinPlanOperator
{
    public MergeSortJoinPlanOperator(PlanOperator left, ExpressionNode expression, PlanOperator right) : base(left, expression, right)
    {
    }
    
    public static MergeSortJoinPlanOperator FromJoinPop(JoinPlanOperator joinPop)
    {
        return new MergeSortJoinPlanOperator(joinPop.Left, joinPop.Expression, joinPop.Right)
        {
            DistributionData = joinPop.DistributionData,
        };
    }

    protected override string GetNodeInfo() => $"JOIN [MERGE SORT]: {Expression}";
    protected override string GetNodeInfoHtml() =>
        $"{HtmlClasses.Bold(HtmlClasses.Colored("JOIN", color: "green"))} [{HtmlClasses.Italic(HtmlClasses.Colored("MERGE SORT", color: "gray"))}]: {Expression}";
}
