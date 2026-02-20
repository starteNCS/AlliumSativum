using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Optimize;

public sealed class WhereOptimizer
{
    private readonly ExpressionNodeOptimizer _expressionNodeOptimizer;
    private readonly ICostModel _costModel;

    public WhereOptimizer(
        ExpressionNodeOptimizer expressionNodeOptimizer,
        ICostModel costModel)
    {
        _expressionNodeOptimizer = expressionNodeOptimizer;
        _costModel = costModel;
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
            var tables = clause.GetTablesOfExpression();
            
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
    public async Task<PlanOperator> DistributeWhereToProposalsAsync(PlanContainer scan, SelectBaseModel onPremise, SelectBaseModel? unplanned)
    {
        var (unplannedExprLeft, unplannedExpr) = _expressionNodeOptimizer.ExtractExpression(unplanned?.Where, scan.PlannedItems.AffectedTables);
        if (unplannedExpr is null)
        {
            // unplanned.Where is later assigned to OnPremise, or later POP's
            return scan.Plan;
        }
        
        (onPremise.Where, var onPremiseExpr) = _expressionNodeOptimizer.ExtractExpression(onPremise.Where, scan.PlannedItems.AffectedTables);
        unplanned?.Where = unplannedExprLeft;
        
        // could be wrapped as WherePOP(WherePOP()), but reduce the nesting by "AND" combining them
        var mergedExpr = _expressionNodeOptimizer.MergeCnfExpressions(onPremiseExpr, unplannedExpr);
        if (mergedExpr is null)
        {
            return scan.Plan;
        }

        var previousCardinality = scan.Plan.ExpectedCardinality;
        var (cardinality, selectivity) = await _costModel.CalculateExpectedCardinalityAsync((BinaryOperatorExpressionNode)mergedExpr, previousCardinality);
        
        var filterPop = new FilterPlanOperator(mergedExpr)
        {
            Children = [scan.Plan],
            ExpectedCardinality = cardinality,
            Selectivity = selectivity
        };
        filterPop.Cost = _costModel.CalculateCost(filterPop);

        return filterPop;
    }
}
