using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize;

public partial class Optimizer
{
    /// <summary>
    /// Iterates through the WHERE expression tree to check if they could be pushed down to a proposal
    /// </summary>
    /// <param name="onPremise"></param>
    /// <param name="joinedTableProposals"></param>
    private void AssignWhereToJoinedProposals(SelectBaseModel onPremise, List<SelectBaseModel> joinedTableProposals)
    {
        if (onPremise.Where is null)
        {
            return;
        }
        
        var clauses = GetCnfSubTrees(onPremise.Where);
        foreach (var clause in clauses)
        {
            var tables = GetTablesOfExpression(clause);
            
            var potentialProposal = joinedTableProposals.Find(p => tables.TrueForAll(t  => p.AffectedTables.Contains(t)));
            if (potentialProposal is null)
            {
                // no possible push down proposal, since the expression affects more tables than the proposal
                continue;
            }
            
            MergeCnfExpressions(potentialProposal.Where, clause);
            onPremise.Where = RemoveCnfExpression(onPremise.Where, clause);
        }
    }

    /// <summary>
    /// Distributes both all possible where sub-trees from onPremise, and also all non-accepted where from proposal
    /// </summary>
    /// <param name="scan"></param>
    /// <param name="onPremise"></param>
    /// <param name="proposalAffectedTables"></param>
    /// <param name="unplanned"></param>
    /// <returns></returns>
    private PlanOperator DistributeWhereToProposals(PlanOperator scan, SelectBaseModel onPremise, List<TableSpecifier> proposalAffectedTables, SelectBaseModel? unplanned)
    {
        (onPremise.Where, var onPremiseExpr) = ExtractExpression(onPremise.Where, proposalAffectedTables);
        
        // could be wrapped as WherePOP(WherePOP()), but reduce the nesting by "AND" combining them
        var mergedExpr = MergeCnfExpressions(onPremiseExpr, unplanned?.Where);
        if (mergedExpr is null)
        {
            return scan;
        }

        return new WherePlanOperator(mergedExpr)
        {
            Children = [scan]
        };
    }
}
