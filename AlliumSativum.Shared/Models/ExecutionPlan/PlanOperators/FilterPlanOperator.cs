using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class FilterPlanOperator : PlanOperator
{
    public ExpressionNode Expression { get; }

    public FilterPlanOperator(ExpressionNode expression)
    {
        Expression = expression;
    }
    
    protected override string GetNodeInfo() => $"FILTER: {Expression}";
    protected override string GetNodeInfoHtml() => $"{HtmlClasses.Bold(HtmlClasses.Colored("FILTER", color: "crimson"))}: {HtmlClasses.Italic(Expression?.ToString())}";
    protected override double GetActualSelectivityInfo() => (double) ExecutionData.ActualCardinality / Children.Single().ExecutionData.ActualCardinality;
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Expression);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FilterPlanOperator other)
        {
            return false;
        }
        
        return other.Expression.Equals(Expression);
    }
}