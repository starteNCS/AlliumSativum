using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class FilterPlanOperator : PlanOperator
{
    public FilterPlanOperator(ExpressionNode expression)
    {
        Expression = expression;
    }

    public ExpressionNode Expression { get; }

    protected override string GetNodeInfo()
    {
        return $"FILTER: {Expression}";
    }

    protected override string GetNodeInfoHtml()
    {
        return
            $"{HtmlClasses.Bold(HtmlClasses.Colored("FILTER", "crimson"))}: {HtmlClasses.Italic(Expression?.ToString())}";
    }

    protected override double GetActualSelectivityInfo()
    {
        return (double)ExecutionData.ActualCardinality / Children.Single().ExecutionData.ActualCardinality;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Expression);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FilterPlanOperator other) return false;

        return other.Expression.Equals(Expression);
    }
}