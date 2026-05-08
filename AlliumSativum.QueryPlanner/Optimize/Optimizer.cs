using System.Diagnostics;
using AlliumSativum.Interfaces;
using AlliumSativum.Optimize.Interfaces;
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

public sealed class Optimizer : IOptimizer
{
    private readonly ICostModel _costModel;
    private readonly IExpressionNodeOptimizer _expressionNodeOptimizer;
    private readonly IJoinOptimizer _joinOptimizer;
    private readonly IPlannerApi _planner;
    private readonly ISelectOptimizer _selectOptimizer;
    private readonly IWhereOptimizer _whereOptimizer;

    public Optimizer(
        IPlannerApi planner,
        IExpressionNodeOptimizer expressionNodeOptimizer,
        IJoinOptimizer joinOptimizer,
        ISelectOptimizer selectOptimizer,
        IWhereOptimizer whereOptimizer,
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
    ///     Optimizes the given SelectBaseModel into a QueryExecutionPlan
    ///     Operates in multiple steps:
    ///     (✅ implementation, ☑️ test)
    ///     - create on-premise only join tree ✅ ☑️
    ///     - split the given model into TABLES ✅ ☑️
    ///     - check which WHERE expressions can be 100% assigned to one table ✅ ☑️
    ///     - append hidden selects ✅ ☑️
    ///     - check joins, merge multiple tables into one sub plan if possible ✅ ☑️
    ///     - check WHERE again, if any more can be pushed down ✅
    ///     - propose to the worker ✅
    ///     - check what it did not accept and add POP's to the plan accordingly
    ///     - Join Order Optimization of on-premise joins
    ///     - rule/cost-based check what POP's can be accumulated for cost reduction (if any)
    ///     - accumulate cost
    ///     - return plan with cost
    ///     -
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="AsSqlOptimizeException"></exception>
    public async Task<List<QueryExecutionPlan>> OptimizeAsync(SelectDto model, bool prune = true)
    {
        var stopwatch = Stopwatch.StartNew();
        var projections = new HashSet<AttributeSpecifier>();
        foreach (var select in model.Select) projections.Add((AttributeSpecifier)select);

        // create on-premise only join tree
        var (onPremiseJoins, additionalSelectAttributesNeededForJoin) = _joinOptimizer.ExtractOnPremiseJoins(model);
        foreach (var select in additionalSelectAttributesNeededForJoin) projections.Add(select);


        // split the given model into TABLES
        var (onPremise, tableSplits) = SplitIntoTables(model, projections);
        tableSplits = _selectOptimizer.AppendComputationalSelects(tableSplits, additionalSelectAttributesNeededForJoin);

        // check joins, merge multiple tables into one sub plan if possible
        var (joinsLeftOnPremise, joinedTableSelect) =
            _joinOptimizer.CombineTablesByJoinPushDown(onPremise.Join, tableSplits);
        onPremise.Join = joinsLeftOnPremise;

        // check WHERE again, if any more can be pushed down
        _whereOptimizer.AssignWhereToJoinedProposals(onPremise, joinedTableSelect);

        var plans = new PopLookupTable();
        // propose to the worker
        foreach (var select in joinedTableSelect)
        {
            var (plannedProposals, unplanned) = await _planner.PlanQueryAsync(select);
            if (plannedProposals.Count == 0) throw new AsSqlOptimizeException("Expected pushdown plan, but got none");

            // add all projections. Since projections is a hash map, duplicates are not an issue, but hidden
            // attributes are added as well, which is important for the final projection at the end of the optimization process
            foreach (var x in unplanned?.Select ?? []) projections.Add((AttributeSpecifier)x);

            foreach (var plannedProposal in plannedProposals)
            {
                var wrappedResult = await WrapPlanProposalWithMissingPopsAsync(plannedProposal, onPremise, unplanned);
                plans.Add(wrappedResult.PlannedItems.AffectedTables, wrappedResult.Plan);
            }

            // could not distribute WHERE fully, therefore the missing part needs to be added to the on-premise plan
            if (unplanned?.Where is not null)
                onPremise.Where = _expressionNodeOptimizer.MergeCnfExpressions(onPremise.Where, unplanned.Where);

            // push all joins left to the intermediate join tree, so that they can be optimized together with the on-premise joins.
            // (where in fact, those joins are now also on-premise, since they cannot be executed at the worker without the planned tables)
            foreach (var join in unplanned?.Join ?? [])
            {
                onPremiseJoins.Add(join);
                foreach (var attr in join.Expression.GetAttributesOfExpression()) projections.Add(attr);
            }
        }

        var joinPlans =
            await _joinOptimizer.ConstructJoinPopTreeFromIntermediateJoinTreeAsync(onPremiseJoins, plans, prune);

        for (var i = 0; i < joinPlans.Count; i++)
        {
            var joinPlanRoot = joinPlans[i];
            if (onPremise.Where is not null)
            {
                var distributionCost =
                    await _costModel.GetDistributionOfExpressionAsync((BinaryOperatorExpressionNode)onPremise.Where,
                        joinPlanRoot.DistributionData,
                        joinPlanRoot.Children);
                joinPlanRoot = new FilterPlanOperator(onPremise.Where)
                {
                    Children = [joinPlanRoot],
                    ExpectedCardinality = distributionCost.Cardinality,
                    Selectivity = distributionCost.Selectivity,
                    DistributionData = distributionCost.Distribution,
                    Width = joinPlanRoot.Width
                };
                joinPlanRoot.Cost = _costModel.CalculateCost(joinPlanRoot);
            }

            // if there is any Hidden attribute, get rid of it here by projecting to only non-Hidden attributes
            if (projections.Any(p => p.IsHidden))
            {
                var applyProjections = projections.Where(x => !x.IsHidden).ToList();
                joinPlanRoot = new ProjectPlanOperator(applyProjections)
                {
                    Children = [joinPlanRoot],
                    ExpectedCardinality = joinPlanRoot.ExpectedCardinality,
                    DistributionData = joinPlanRoot.DistributionData
                        .Where(x => applyProjections.Contains(x.Key))
                        .ToDictionary(x => x.Key, x => x.Value),
                    Width = applyProjections.Count
                };
                joinPlanRoot.Cost = _costModel.CalculateCost(joinPlanRoot);
            }

            joinPlans[i] = joinPlanRoot;
        }

        stopwatch.Stop();
        return joinPlans
            .Select(x => new QueryExecutionPlan
            {
                TotalCost = _costModel.TotalCost(x),
                OptimizeTimeMs = stopwatch.ElapsedMilliseconds,
                RootOperator = x
            }).ToList();
    }

    private async Task<PlanContainer> WrapPlanProposalWithMissingPopsAsync(PlanContainer planContainer,
        SelectDto onPremise, SelectDto? unplanned)
    {
        // check if there are any pops, that are now exclusive to this proposal
        planContainer.Plan =
            _selectOptimizer.HandleProjection(planContainer.Plan, planContainer.PlannedItems.From, unplanned);
        planContainer.Plan = await _whereOptimizer.DistributeWhereToProposalsAsync(planContainer, onPremise, unplanned);

        return planContainer;
    }

    /// <summary>
    ///     Splits the provided (already parsed) query into multiple SelectBaseModels, one for each data source respectively
    /// </summary>
    /// <param name="model"></param>
    /// <returns>
    ///     - onPremise: whatever was not able to be split for data sources
    ///     - dataSources: the parts which should be checked for push down
    /// </returns>
    public (SelectDto onPremise, List<SelectDto> dataSources) SplitIntoTables(SelectDto model,
        HashSet<AttributeSpecifier> allProjections)
    {
        // new data sources may only be introduced in either JOIN or FROM
        var tables = model.Join
            .Select(j => j.Inner)
            .Append(model.From!);

        if (model.Where is not null) model.Where = BooleanExpressionParser.AsConjunctiveNormalForm(model.Where);


        List<SelectDto> selects = [];
        foreach (var table in tables)
        {
            var (@base, split) = ExtractTable(model, table, allProjections);
            model = @base;
            selects.Add(split);
        }

        return (model, selects);
    }

    /// <summary>
    ///     Extracts a single table of the given SelectBaseModel
    /// </summary>
    /// <param name="model">Base model</param>
    /// <param name="table">Which table should be extracted</param>
    /// <returns>
    ///     - base: what is left of the base model
    ///     - split: the split for this specific table
    /// </returns>
    private (SelectDto @base, SelectDto split) ExtractTable(SelectDto model, TableSpecifier table,
        HashSet<AttributeSpecifier> allProjections)
    {
        var extractedWhere = _expressionNodeOptimizer.ExtractExpression(model.Where, table);
        var hiddenSplitWhereAttributes = extractedWhere.split?.GetAttributesOfExpression() ?? [];

        var selectModel = new SelectDto
        {
            From = model.From,
            Join = model.Join,
            Where = extractedWhere.@base,
            Select = model.Select
        };

        var split = new SelectDto
        {
            From = table,
            Select = model.Select
                .Where(spec => spec is AttributeSpecifier aSpec && aSpec.IsInTable(table))
                .ToList()
                .AppendHiddenAttributes(hiddenSplitWhereAttributes),
            Join = [],
            Where = extractedWhere.split
        };

        foreach (var attr in hiddenSplitWhereAttributes) allProjections.Add(attr);

        return (selectModel, split);
    }
}

public static class OptimizerExtensions
{
    public static IServiceCollection AddOptimizer(this IServiceCollection services)
    {
        services.AddScoped<IOptimizer, Optimizer>();
        services.AddScoped<IExpressionNodeOptimizer, ExpressionNodeOptimizer>();
        services.AddScoped<IJoinOptimizer, JoinOptimizer>();
        services.AddScoped<ISelectOptimizer, SelectOptimizer>();
        services.AddScoped<IWhereOptimizer, WhereOptimizer>();

        return services;
    }
}