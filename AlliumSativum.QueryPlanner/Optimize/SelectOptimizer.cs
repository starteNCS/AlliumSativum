using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize;

public sealed class SelectOptimizer
{
    /// <summary>
    /// Appends the select projections needed for computational purposes (i.e. join) to all push down proposals
    /// </summary>
    /// <param name="splitSelects">The selects only targetting one table</param>
    /// <param name="hiddenAttributes">The attributes that should be appended</param>
    /// <returns></returns>
    public List<SelectBaseModel> AppendComputationalSelects(List<SelectBaseModel> splitSelects, List<AttributeSpecifier> hiddenAttributes)
    {
        foreach (var attribute in hiddenAttributes)
        {
            var select = splitSelects.SingleOrDefault(s => attribute.IsInTable(s.From));
            if (select is null)
            {
                throw new AsSqlOptimizeException("Expected to find select model to push hidden attribute to");
            }

            if (select.Select.Any(s => s is AttributeSpecifier aSpec && aSpec.Equals(attribute)))
            {
                // model already contains specific select
                continue;
            }

            attribute.IsHidden = true;
            select.Select.Add(attribute);
        }
        
        return splitSelects;
    } 
        
    public PlanOperator HandleProjection(PlanOperator pop, TableSpecifier forTable, SelectBaseModel? unplanned)
    {
        if (unplanned is null || unplanned.Select.Count == 0)
        {
            return pop;
        }

        var projected = unplanned.Select
            .OfType<AttributeSpecifier>()
            .Where(x => x.IsInTable(forTable))
            .ToList();
        unplanned.Select = unplanned.Select
            .Where(x => x is AttributeSpecifier aSpec && !aSpec.IsInTable(forTable))
            .ToList();
        
        return new ProjectPlanOperator(projected)
        {
            Children = [pop],
            ExpectedCardinality = pop.ExpectedCardinality,
        };
    }
}
