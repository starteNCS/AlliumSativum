using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Shared.Models.ExecutionPlan;

public sealed class PlanContainer
{
    public required PlanOperator Plan { get; set; }
    public required SelectBaseModel PlannedItems { get; set; }
}
