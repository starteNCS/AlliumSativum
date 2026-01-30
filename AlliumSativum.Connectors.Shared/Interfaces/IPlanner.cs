using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Connectors.Shared.Interfaces;

public interface IPlanner
{
    /// <summary>
    /// Plans the given subset of a select model
    /// </summary>
    /// <param name="selectModel"></param>
    /// <returns></returns>
    Task<QueryExecutionPlan?> PlanAsync(Guid dataSource, SelectBaseModel selectModel);
}
