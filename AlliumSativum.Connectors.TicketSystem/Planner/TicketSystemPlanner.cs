using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Connectors.TicketSystem.Planner;

public sealed class TicketSystemPlanner : IPlanner
{
    public Task<PlanOperator?> PlanAsync(Guid dataSource, SelectBaseModel selectModel)
    {
        throw new NotImplementedException();
    }
}
