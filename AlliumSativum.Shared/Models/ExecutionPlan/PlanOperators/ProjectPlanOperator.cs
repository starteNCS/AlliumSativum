using System.Text;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class ProjectPlanOperator : PlanOperator
{
    public List<AttributeSpecifier> Attributes { get; }
    
    public ProjectPlanOperator(params List<AttributeSpecifier> attributes)
    {
        Attributes = attributes;
    }
    
    protected override string GetNodeInfo() => $"PROJECT: {string.Join(", ", Attributes.Select(x => 
        new StringBuilder()
            .Append(x.IsHidden ? "{" : "")
            .Append(x.AttributeName)
            .Append(x.IsHidden ? "}" : "").ToString()
    ))}";
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Attributes);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ProjectPlanOperator other)
        {
            return false;
        }
        
        return other.Attributes.All(x => Attributes.Contains(x));
    }
}