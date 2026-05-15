using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Optimize.Interfaces;

public interface IWhereOptimizer
{
    /// <summary>
    /// Distributes all CNF subtrees of the WHERE clause of unplanned and onPremise to the proposals, if possible. Returns the new plan with the WHERE expressions distributed.
    /// </summary>
    /// <remarks>
    /// Can only distribute if ALL tables of the CNF subtree are affected by the proposal
    /// </remarks>
    /// <param name="scan">The POP loading the data</param>
    /// <param name="onPremise">The base select dto</param>
    /// <param name="unplanned">The unplanned part of a worker proposal</param>
    /// <returns>A wrapped POP, or scan</returns>
    Task<PlanOperator> DistributeWhereToProposalsAsync(PlanContainer scan, SelectDto onPremise,
        SelectDto? unplanned);

    /// <summary>
    /// Iterates through the WHERE expression tree to check if they could be pushed down to a proposal.
    /// </summary>
    /// <remarks>
    /// Distribution is in place
    /// </remarks>
    /// <param name="onPremise">The base select dto</param>
    /// <param name="joinedTableProposals">The proposals</param>
    void AssignWhereToJoinedProposals(SelectDto onPremise, List<SelectDto> joinedTableProposals);
}