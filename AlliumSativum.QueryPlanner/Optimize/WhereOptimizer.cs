using AlliumSativum.Optimize.Interfaces;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Optimize;

public sealed class WhereOptimizer : IWhereOptimizer
{
    private readonly ICostModel _costModel;
    private readonly IExpressionNodeOptimizer _expressionNodeOptimizer;

    public WhereOptimizer(
        IExpressionNodeOptimizer expressionNodeOptimizer,
        ICostModel costModel)
    {
        _expressionNodeOptimizer = expressionNodeOptimizer;
        _costModel = costModel;
    }

    /// <summary>
    ///     Iterates through the WHERE expression tree to check if they could be pushed down to a proposal
    /// </summary>
    /// <param name="onPremise"></param>
    /// <param name="joinedTableProposals"></param>
    public void AssignWhereToJoinedProposals(SelectDto onPremise, List<SelectDto> joinedTableProposals)
    {
        if (onPremise.Where is null) return;

        var clauses = _expressionNodeOptimizer.GetCnfSubTrees(onPremise.Where);
        foreach (var clause in clauses)
        {
            var tables = clause.GetTablesOfExpression();

            var potentialProposal =
                joinedTableProposals.Find(p => tables.TrueForAll(t => p.AffectedTables.Contains(t)));
            if (potentialProposal is null)
                // no possible push down proposal, since the expression affects more tables than the proposal
                continue;

            potentialProposal.Where = _expressionNodeOptimizer.MergeCnfExpressions(potentialProposal.Where, clause);
            onPremise.Where = _expressionNodeOptimizer.RemoveCnfExpression(onPremise.Where, clause);
        }
    }

    
    public async Task<PlanOperator> DistributeWhereToProposalsAsync(PlanContainer scan, SelectDto onPremise,
        SelectDto? unplanned)
    {
        var (unplannedExprLeft, unplannedExpr) =
            _expressionNodeOptimizer.ExtractExpression(unplanned?.Where, scan.PlannedItems.AffectedTables);
        if (unplannedExpr is null)
            // unplanned.Where is later assigned to OnPremise, or later POP's
            return scan.Plan;

        (onPremise.Where, var onPremiseExpr) =
            _expressionNodeOptimizer.ExtractExpression(onPremise.Where, scan.PlannedItems.AffectedTables);
        unplanned?.Where = unplannedExprLeft;

        // could be wrapped as WherePOP(WherePOP()), but reduce the nesting by "AND" combining them
        var mergedExpr = _expressionNodeOptimizer.MergeCnfExpressions(onPremiseExpr, unplannedExpr);
        if (mergedExpr is null) return scan.Plan;

        var distributionCost = await _costModel.GetDistributionOfExpressionAsync(
            (BinaryOperatorExpressionNode)mergedExpr,
            scan.Plan.DistributionData,
            scan.Plan.Children);

        var filterPop = new FilterPlanOperator(mergedExpr)
        {
            Children = [scan.Plan],
            ExpectedCardinality = distributionCost.Cardinality,
            Selectivity = distributionCost.Selectivity,
            DistributionData = distributionCost.Distribution,
            Width = scan.Plan.Width
        };

        filterPop.Cost = _costModel.CalculateCost(filterPop);

        return filterPop;
    }
}