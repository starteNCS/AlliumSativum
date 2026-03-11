using System.Diagnostics;
using AlliumSativum.Parser.Algorithms;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Shared.Utils;
using AlliumSativum.Worker.Sdk;
using Microsoft.Extensions.DependencyInjection;

namespace AlliumSativum.Optimize;

public sealed class Optimizer
{
    private readonly IPlannerApi _planner;
    private readonly ExpressionNodeOptimizer _expressionNodeOptimizer;
    private readonly JoinOptimizer _joinOptimizer;
    private readonly SelectOptimizer _selectOptimizer;
    private readonly WhereOptimizer _whereOptimizer;
    private readonly ICostModel _costModel;

    public Optimizer(
        IPlannerApi planner,
        ExpressionNodeOptimizer expressionNodeOptimizer,
        JoinOptimizer joinOptimizer,
        SelectOptimizer selectOptimizer,
        WhereOptimizer whereOptimizer,
        ICostModel costModel)
    {
        _planner = planner;
        _expressionNodeOptimizer = expressionNodeOptimizer;
        _joinOptimizer = joinOptimizer;
        _selectOptimizer = selectOptimizer;
        _whereOptimizer = whereOptimizer;
        _costModel = costModel;
    }
    
    /// <summary>
    /// Optimizes the given SelectBaseModel into a QueryExecutionPlan
    /// Operates in multiple steps:
    /// (✅ implementation, ☑️ test)
    /// - create on-premise only join tree ✅ ☑️
    /// - split the given model into TABLES ✅ ☑️
    /// - check which WHERE expressions can be 100% assigned to one table ✅ ☑️ 
    /// - append hidden selects ✅ ☑️
    /// - check joins, merge multiple tables into one sub plan if possible ✅ ☑️
    /// - check WHERE again, if any more can be pushed down ✅
    /// - propose to the worker ✅
    /// - check what it did not accept and add POP's to the plan accordingly
    /// - Join Order Optimization of on-premise joins
    /// - rule/cost-based check what POP's can be accumulated for cost reduction (if any) 
    /// - accumulate cost
    /// - return plan with cost
    /// - 
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="AsSqlOptimizeException"></exception>
    public async Task<QueryExecutionPlan> OptimizeAsync(SelectBaseModel model)
    {
        var stopwatch = Stopwatch.StartNew();
        var projections = new HashSet<AttributeSpecifier>();
        foreach (var select in model.Select)
        {
            projections.Add((AttributeSpecifier) select);
        }
        
        // create on-premise only join tree
        var (onPremiseJoins, additionalSelectAttributesNeededForJoin) = _joinOptimizer.ConstructOnPremiseJoin(model);
        foreach (var select in additionalSelectAttributesNeededForJoin)
        {
            projections.Add(select);
        }
        
        
        // split the given model into TABLES
        var (onPremise, tables) = SplitIntoTables(model, projections);
        tables = _selectOptimizer.AppendComputationalSelects(tables, additionalSelectAttributesNeededForJoin);
        
        // check joins, merge multiple tables into one sub plan if possible
        var (joinsLeftOnPremise, joinedTableSelect) = _joinOptimizer.CombineTablesByJoinPushDown(onPremise.Join, tables);
        onPremise.Join = joinsLeftOnPremise;
        
        // check WHERE again, if any more can be pushed down
        _whereOptimizer.AssignWhereToJoinedProposals(onPremise, joinedTableSelect);

        var plans = new PopLookupTable();
        // propose to the worker
        foreach (var select in joinedTableSelect)
        {
            var (plannedProposals, unplanned) = await _planner.PlanQueryAsync(select);
            if (plannedProposals.Count == 0)
            {
                throw new AsSqlOptimizeException("Expected pushdown plan, but got none");
            }
            
            // add all projections. Since projections is a hash map, duplicates are not an issue, but hidden
            // attributes are added as well, which is important for the final projection at the end of the optimization process
            foreach (var x in unplanned?.Select ?? [])
            {
                projections.Add((AttributeSpecifier)x);
            }

            foreach (var plannedProposal in plannedProposals)
            {
                var wrappedResult = await WrapPlanProposalWithMissingPopsAsync(plannedProposal, onPremise, unplanned);
                plans.Add(wrappedResult.PlannedItems.AffectedTables, wrappedResult.Plan);
            }
            
            // could not distribute WHERE fully, therefore the missing part needs to be added to the on-premise plan
            if (unplanned?.Where is not null)
            {
                onPremise.Where = _expressionNodeOptimizer.MergeCnfExpressions(onPremise.Where, unplanned.Where);
            }

            // push all joins left to the intermediate join tree, so that they can be optimized together with the on-premise joins.
            // (where in fact, those joins are now also on-premise, since they cannot be executed at the worker without the planned tables)
            foreach (var join in unplanned?.Join ?? [])
            {
                (onPremiseJoins, var attributes) = _joinOptimizer.AddJoinToIntermediateJoinTree(onPremiseJoins, join);
                foreach (var attr in attributes)
                {
                    projections.Add(attr);
                }
            }
        }
        
        var joinPlans = await _joinOptimizer.ConstructJoinPopTreeFromIntermediateJoinTreeAsync(onPremiseJoins, plans);
        var asdfasdf = joinPlans.Select(y => _costModel.TotalCost(y)).ToList();
        
        var planRoot = joinPlans[0];

        if (onPremise.Where is not null)
        {
            var (cardinality, _) =
                await _costModel.CalculateExpectedCardinalityAsync((BinaryOperatorExpressionNode)onPremise.Where,
                    planRoot.ExpectedCardinality);
            var (distribution, selectivity) =
                await _costModel.GetDistributionOfExpressionAsync((BinaryOperatorExpressionNode)onPremise.Where,
                    planRoot.DistributionData);
            planRoot = new FilterPlanOperator(onPremise.Where)
            {
                Children = [planRoot],
                ExpectedCardinality = cardinality,
                Selectivity = selectivity,
                DistributionData = distribution,
            };
            planRoot.Cost = _costModel.CalculateCost(planRoot);
        }

        // if there is any Hidden attribute, get rid of it here by projecting to only non-Hidden attributes
        if (projections.Any(p => p.IsHidden))
        {
            var applyProjections = projections.Where(x => !x.IsHidden).ToList();
            planRoot = new ProjectPlanOperator(applyProjections)
            {
                Children = [planRoot],
                ExpectedCardinality = planRoot.ExpectedCardinality,
                DistributionData = planRoot.DistributionData
                    .Where(x => applyProjections.Contains(x.Key))
                    .ToDictionary(x => x.Key, x => x.Value)
            };
            planRoot.Cost = _costModel.CalculateCost(planRoot);
        }
        
        stopwatch.Stop();
        return new QueryExecutionPlan()
        {
            TotalCost = _costModel.TotalCost(planRoot),
            OptimizeTimeMs = stopwatch.ElapsedMilliseconds,
            RootOperator = planRoot
        };
    }

    private async Task<PlanContainer> WrapPlanProposalWithMissingPopsAsync(PlanContainer planContainer, SelectBaseModel onPremise, SelectBaseModel? unplanned)
    {
        // check if there are any pops, that are now exclusive to this proposal (TODO: PUSH DOWN AGAIN?)
        planContainer.Plan = _selectOptimizer.HandleProjection(planContainer.Plan, planContainer.PlannedItems.From, unplanned);
        planContainer.Plan = await _whereOptimizer.DistributeWhereToProposalsAsync(planContainer, onPremise, unplanned);

        return planContainer;
    }

    /// <summary>
    /// Splits the provided (already parsed) query into multiple SelectBaseModels, one for each data source respectively
    /// </summary>
    /// <param name="model"></param>
    /// <returns>
    ///     - onPremise: whatever was not able to be split for data sources
    ///     - dataSources: the parts which should be checked for push down
    /// </returns>
    public (SelectBaseModel onPremise, List<SelectBaseModel> dataSources) SplitIntoTables(SelectBaseModel model, HashSet<AttributeSpecifier> allProjections)
    {
        // new data sources may only be introduced in either JOIN or FROM
        var tables = model.Join
            .Select(j => j.Inner)
            .Append(model.From!);

        if (model.Where is not null)
        {
            model.Where = BooleanExpressionParser.AsConjunctiveNormalForm(model.Where);
        }
        

        List<SelectBaseModel> selects = [];
        foreach (var table in tables)
        {
            var (@base, split) = ExtractTable(model, table, allProjections);
            model = @base;
            selects.Add(split);
        }

        return (model, selects);
    }

    /// <summary>
    /// Extracts a single table of the given SelectBaseModel
    /// </summary>
    /// <param name="model">Base model</param>
    /// <param name="table">Which table should be extracted</param>
    /// <returns>
    ///     - base: what is left of the base model
    ///     - split: the split for this specific table
    /// </returns>
    private (SelectBaseModel @base, SelectBaseModel split) ExtractTable(SelectBaseModel model, TableSpecifier table, HashSet<AttributeSpecifier> allProjections)
    {
        
        var extractedWhere = _expressionNodeOptimizer.ExtractExpression(model.Where, table);
        var hiddenSplitWhereAttributes = extractedWhere.split?.GetAttributesOfExpression() ?? [];
        
        var selectModel = new SelectBaseModel
        {
            From = model.From,
            Join = model.Join,
            Where = extractedWhere.@base,
            Select = model.Select,
        };

        var split = new SelectBaseModel()
        {
            From = table,
            Select = model.Select
                .Where(spec => spec is AttributeSpecifier aSpec && aSpec.IsInTable(table))
                .ToList()
                .AppendHiddenAttributes(hiddenSplitWhereAttributes),
            Join = [], 
            Where = extractedWhere.split
        };

        foreach (var attr in hiddenSplitWhereAttributes)
        {
            allProjections.Add(attr);
        }
        
        return (selectModel, split);
    }
}

public static class OptimizerExtensions
{
    public static IServiceCollection AddOptimizer(this IServiceCollection services)
    {
        services.AddScoped<Optimizer>();
        services.AddScoped<ExpressionNodeOptimizer>();
        services.AddScoped<JoinOptimizer>();
        services.AddScoped<SelectOptimizer>();
        services.AddScoped<WhereOptimizer>();
        
        return services;
    }
}
