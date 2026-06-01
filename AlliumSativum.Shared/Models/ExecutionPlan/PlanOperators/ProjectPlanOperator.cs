using System.Text;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class ProjectPlanOperator : PlanOperator
{
    public ProjectPlanOperator(params List<AttributeSpecifier> attributes)
    {
        Attributes = attributes;
    }

    public List<AttributeSpecifier> Attributes { get; }

    protected override string GetNodeInfo()
    {
        return $"PROJECT: {string.Join(", ", Attributes.Select(x =>
            new StringBuilder()
                .Append(x.IsHidden ? "{" : "")
                .Append(x.AttributeName)
                .Append(x.IsHidden ? "}" : "").ToString()
        ))}";
    }

    protected override string GetNodeInfoHtml()
    {
        return
            $"{HtmlClasses.Bold(HtmlClasses.Colored("PROJECT", "purple"))}: {string.Join(", ", Attributes.Select(x =>
                new StringBuilder()
                    .Append(x.IsHidden ? "{" : "")
                    .Append(x.AttributeName)
                    .Append(x.IsHidden ? "}" : "").ToString()
            ))}";
    }

    protected override double GetActualSelectivityInfo()
    {
        return 1;
    }

    public override string ToJoinPlanString()
    {
        return Children.Single().ToJoinPlanString();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Attributes);
    }

    public override bool Equals(object? other)
    {
        if (other is not ProjectPlanOperator project) return false;

        return project.Attributes.All(x => Attributes.Contains(x));
    }

    public override bool IsEquivalentTo(PlanOperator? other)
    {
        if (!base.IsEquivalentTo(other)) return false;
        return other is ProjectPlanOperator otherProject && Equals(otherProject);
    }
}