using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.STAR.Terminals;

public abstract class PlanOperator : ISymbol
{
    public List<PlanOperator> Children { get; set; }

    public abstract PlanOperator Develop(SelectBaseModel select);
}