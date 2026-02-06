using System.Linq.Expressions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Optimize;

public partial class Optimizer
{
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
