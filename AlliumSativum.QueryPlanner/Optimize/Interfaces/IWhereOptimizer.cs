using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Optimize.Interfaces;

public interface IWhereOptimizer
{
    /// <summary>
    ///     Distributes both all possible where sub-trees from onPremise, and also all non-accepted where from proposal
    /// </summary>
    /// <param name="scan"></param>
    /// <param name="onPremise"></param>
    /// <param name="proposalAffectedTables"></param>
    /// <param name="unplanned"></param>
    /// <returns></returns>
    Task<PlanOperator> DistributeWhereToProposalsAsync(PlanContainer scan, SelectBaseModel onPremise,
        SelectBaseModel? unplanned);

    /// <summary>
    ///     Iterates through the WHERE expression tree to check if they could be pushed down to a proposal
    /// </summary>
    /// <param name="onPremise"></param>
    /// <param name="joinedTableProposals"></param>
    void AssignWhereToJoinedProposals(SelectBaseModel onPremise, List<SelectBaseModel> joinedTableProposals);
}