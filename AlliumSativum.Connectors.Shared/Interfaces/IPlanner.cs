using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Connectors.Shared.Interfaces;

public interface IPlanner
{
    /// <summary>
    ///     Plans the given subset of a select model
    ///     May either return a single plan when all operations can be executed,
    ///     or multiple plans when some join cannot be executed on this data source.
    ///     When multiple plans are returned, the unplanned.Select MUST contain all attributes needed for the join and filters
    ///     as a hidden attribute (IsHidden = true)
    /// </summary>
    /// <param name="dataSourceId"></param>
    /// <param name="selectModel"></param>
    /// <returns></returns>
    Task<(List<PlanContainer> proposal, SelectDto? unplanned)> PlanAsync(Guid dataSourceId,
        SelectDto selectModel);
}