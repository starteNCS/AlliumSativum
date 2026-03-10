using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.ExecutionPlan;

public abstract partial class PlanOperator
{
    public List<PlanOperator> Children { get; init; } = [];
    public double Cost { get; set; }
    public long ExpectedCardinality { get; set; }
    public double Selectivity { get; set; } = 1;

    public required Dictionary<AttributeSpecifier, PlanOperatorDistributionData> DistributionData { get; set; }
    public PlanOperatorExecutionData ExecutionData { get; set; } = new();
}