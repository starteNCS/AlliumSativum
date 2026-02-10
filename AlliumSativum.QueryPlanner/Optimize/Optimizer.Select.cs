using System.Linq.Expressions;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize;

public partial class Optimizer
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

            if (select.Select.Any(s => s is AttributeSpecifier && attribute.Equals(s)))
            {
                // model already contains specific select
                continue;
            }

            attribute.IsHidden = true;
            select.Select.Add(attribute);
        }
        
        return splitSelects;
    } 
        
    private PlanOperator HandleProjection(PlanOperator pop, SelectBaseModel? unplanned)
    {
        if (unplanned is null || unplanned.Select.Count == 0)
        {
            return pop;
        }
        
        return new ProjectPlanOperator(unplanned.Select)
        {
            Children = [pop]
        };
    }
}
