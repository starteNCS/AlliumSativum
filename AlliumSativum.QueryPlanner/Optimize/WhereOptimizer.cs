using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize;

public sealed class WhereOptimizer
{
    private readonly ExpressionNodeOptimizer _expressionNodeOptimizer;

    public WhereOptimizer(ExpressionNodeOptimizer expressionNodeOptimizer)
    {
        _expressionNodeOptimizer = expressionNodeOptimizer;
    }
    
    /// <summary>
    /// Iterates through the WHERE expression tree to check if they could be pushed down to a proposal
    /// </summary>
    /// <param name="onPremise"></param>
    /// <param name="joinedTableProposals"></param>
    public void AssignWhereToJoinedProposals(SelectBaseModel onPremise, List<SelectBaseModel> joinedTableProposals)
    {
        if (onPremise.Where is null)
        {
            return;
        }
        
        var clauses = _expressionNodeOptimizer.GetCnfSubTrees(onPremise.Where);
        foreach (var clause in clauses)
        {
            var tables = _expressionNodeOptimizer.GetTablesOfExpression(clause);
            
            var potentialProposal = joinedTableProposals.Find(p => tables.TrueForAll(t  => p.AffectedTables.Contains(t)));
            if (potentialProposal is null)
            {
                // no possible push down proposal, since the expression affects more tables than the proposal
                continue;
            }
            
            _expressionNodeOptimizer.MergeCnfExpressions(potentialProposal.Where, clause);
            onPremise.Where = _expressionNodeOptimizer.RemoveCnfExpression(onPremise.Where, clause);
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
    public PlanOperator DistributeWhereToProposals(PlanOperator scan, SelectBaseModel onPremise, List<TableSpecifier> proposalAffectedTables, SelectBaseModel? unplanned)
    {
        (onPremise.Where, var onPremiseExpr) = _expressionNodeOptimizer.ExtractExpression(onPremise.Where, proposalAffectedTables);
        
        // could be wrapped as WherePOP(WherePOP()), but reduce the nesting by "AND" combining them
        var mergedExpr = _expressionNodeOptimizer.MergeCnfExpressions(onPremiseExpr, unplanned?.Where);
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
