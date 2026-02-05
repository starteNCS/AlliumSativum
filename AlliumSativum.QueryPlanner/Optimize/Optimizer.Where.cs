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
    
    private void DistributeOnPremiseWhereToPlans(SelectBaseModel onPremise, List<SelectBaseModel> joinedTableProposals, Dictionary<List<TableSpecifier>, PlanOperator> plans)
    {
        if (onPremise.Where is null)
        {
            return;
        }
        
        foreach (var planProposal in joinedTableProposals)
        {
            (onPremise.Where, var expr) = ExtractExpression(onPremise.Where, planProposal.AffectedTables);

            var scanPlanOperator = plans
                .FirstOrDefault(p => p.Key.Contains(planProposal.From!))
                .Value;
            if (expr is null || scanPlanOperator is null)
            {
                continue;
            }

            var whereOperator = new WherePlanOperator(expr)
            {
                Children = [scanPlanOperator]
            };

            plans.Remove(planProposal.AffectedTables);
            plans[[planProposal.From!]] = whereOperator;

            // if there are no items left in the tree, we do not need to check here further
            if (onPremise.Where is null)
            {
                break;
            }
        }
    }
}
