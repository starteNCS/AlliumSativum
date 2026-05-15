using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize.Interfaces;

public interface ISelectOptimizer
{
    /// <summary>
    /// Appends all projections, that should be hidden.
    /// Hidden attributes are needed for computations, but should not be projected in the end.
    /// Joining attributes on a key are an example of hidden attributes.
    /// </summary>
    /// <param name="tableSplits">The table splits</param>
    /// <param name="hiddenAttributes">The hidden (!) attributes that should be appended</param>
    /// <returns></returns>
    List<SelectDto> AppendComputationalSelects(List<SelectDto> tableSplits,
        List<AttributeSpecifier> hiddenAttributes);

    /// <summary>
    /// Appending a projection operator on top of the given plan operator, if there are any attributes in the unplanned select
    /// </summary>
    /// <param name="pop">The pop to be wrapped</param>
    /// <param name="forTable">The table of the POP</param>
    /// <param name="unplanned">The select dto containing the unplanned projections</param>
    /// <returns></returns>
    PlanOperator HandleProjection(PlanOperator pop, TableSpecifier forTable, SelectDto? unplanned);
}
