using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Costs.Models;

public sealed class PlanOperatorDistributionCost
{
    public Dictionary<AttributeSpecifier, PlanOperatorDistributionData> Distribution { get; set; } = [];
    public double Selectivity { get; set; }
    public long Cardinality { get; set; }
}
