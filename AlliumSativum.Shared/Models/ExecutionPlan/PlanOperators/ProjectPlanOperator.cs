using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class ProjectPlanOperator : PlanOperator
{
    public List<string> Attributes { get; }
    
    public ProjectPlanOperator(params List<string> attributes)
    {
        Attributes = attributes;
    }
    
    public ProjectPlanOperator(params List<ISpecifier> attributes)
    {
        Attributes = attributes
            .Where(x => x is AttributeSpecifier)
            .Select(x => ((AttributeSpecifier)x).AttributeName)
            .ToList();
    }
    
    protected override string GetNodeInfo() => $"({Cost}ms) PROJECT: {string.Join(", ", Attributes)}";
    
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