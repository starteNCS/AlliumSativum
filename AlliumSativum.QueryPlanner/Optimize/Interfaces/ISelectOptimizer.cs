using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize.Interfaces;

public interface ISelectOptimizer
{
    /// <summary>
    ///     Appends the select projections needed for computational purposes (i.e. join) to all push down proposals
    /// </summary>
    /// <param name="splitSelects">The selects only targetting one table</param>
    /// <param name="hiddenAttributes">The attributes that should be appended</param>
    /// <returns></returns>
    List<SelectBaseModel> AppendComputationalSelects(List<SelectBaseModel> splitSelects,
        List<AttributeSpecifier> hiddenAttributes);

    PlanOperator HandleProjection(PlanOperator pop, TableSpecifier forTable, SelectBaseModel? unplanned);
}
