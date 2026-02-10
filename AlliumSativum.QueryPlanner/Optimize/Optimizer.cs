using AlliumSativum.Parser.Algorithms;
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
    private readonly PlannerApi _planner;
    private readonly ExpressionNodeOptimizer _expressionNodeOptimizer;
    private readonly JoinOptimizer _joinOptimizer;
    private readonly SelectOptimizer _selectOptimizer;
    private readonly WhereOptimizer _whereOptimizer;

    public Optimizer(
        PlannerApi planner,
        ExpressionNodeOptimizer expressionNodeOptimizer,
        JoinOptimizer joinOptimizer,
        SelectOptimizer selectOptimizer,
        WhereOptimizer whereOptimizer)
    {
        _planner = planner;
        _expressionNodeOptimizer = expressionNodeOptimizer;
        _joinOptimizer = joinOptimizer;
        _selectOptimizer = selectOptimizer;
        _whereOptimizer = whereOptimizer;
    }
    
    /// <summary>
    /// Optimizes the given SelectBaseModel into a QueryExecutionPlan
    /// Operates in multiple steps:
    ///
    /// - create on-premise only join tree✅
    /// - split the given model into TABLES ✅
    /// - check which WHERE expressions can be 100% assigned to one table ✅
    /// - append hidden selects ✅
    /// - check joins, merge multiple tables into one sub plan if possible ✅
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
    public async Task<QueryExecutionPlan> Optimize(SelectBaseModel model)
    {
        // create on-premise only join tree
        var (onPremiseJoinTree, additionalSelectAttributesNeeded) = _joinOptimizer.ConstructOnPremiseJoin(model);
        
        // split the given model into TABLES
        // check which WHERE expressions can be 100% assigned to one table
        var (onPremise, tables) = SplitIntoTables(model);
        tables = _selectOptimizer.AppendComputationalSelects(tables, additionalSelectAttributesNeeded);
        
        // check joins, merge multiple tables into one sub plan if possible
        var (joinsLeftOnPremise, joinedTableSelect) = _joinOptimizer.CombineTablesByJoinPushDown(onPremise.Join, tables);
        onPremise.Join = joinsLeftOnPremise;
        
        // check WHERE again, if any more can be pushed down
        _whereOptimizer.AssignWhereToJoinedProposals(onPremise, joinedTableSelect);

        var plans = new PopLookupTable();
        // propose to the worker
        foreach (var select in joinedTableSelect)
        {
            var (plan, unplanned) = await _planner.PlanQueryAsync(select);
            if (plan is null)
            {
                throw new AsSqlOptimizeException("Expected pushdown plan, but got none");
            }
            
            var popTree = WrapPlanProposalWithMissingPops(plan, onPremise, select, unplanned);
            plans.Add(select.AffectedTables, popTree);
        }
        
        var planRoot = _joinOptimizer.ConstructJoinPopTreeFromIntermediateJoinTree(onPremiseJoinTree, plans);

        if (onPremise.Where is not null)
        {
            planRoot = new WherePlanOperator(onPremise.Where)
            {
                Children = [planRoot]
            };
        }

        return new QueryExecutionPlan()
        {
            Cost = 1,
            RootOperator = planRoot
        };
    }

    private PlanOperator WrapPlanProposalWithMissingPops(PlanOperator pop, SelectBaseModel onPremise, SelectBaseModel proposalBase, SelectBaseModel? unplanned)
    {
        // check if there are any pops, that are now exclusive to this proposal (TODO: PUSH DOWN AGAIN?)
        pop = _whereOptimizer.DistributeWhereToProposals(pop, onPremise, proposalBase.AffectedTables, unplanned);
        pop = _selectOptimizer.HandleProjection(pop, unplanned);

        return pop;
    }

    /// <summary>
    /// Splits the provided (already parsed) query into multiple SelectBaseModels, one for each data source respectively
    /// </summary>
    /// <param name="model"></param>
    /// <returns>
    ///     - onPremise: whatever was not able to be split for data sources
    ///     - dataSources: the parts which should be checked for push down
    /// </returns>
    private (SelectBaseModel onPremise, List<SelectBaseModel> dataSources) SplitIntoTables(SelectBaseModel model)
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
            var (@base, split) = ExtractTable(model, table);
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
    private (SelectBaseModel @base, SelectBaseModel split) ExtractTable(SelectBaseModel model, TableSpecifier table)
    {
        
        var extractedWhere = _expressionNodeOptimizer.ExtractExpression(model.Where, table);
        
        var selectModel = new SelectBaseModel
        {
            From = model.From,
            Join = model.Join,
            Where = extractedWhere.@base,
            Select = model.Select.Where(spec => spec is AttributeSpecifier aSpec && !aSpec.IsInTable(table)).ToList()
        };

        var split = new SelectBaseModel()
        {
            From = table,
            Select = model.Select.Where(spec => spec is AttributeSpecifier aSpec && aSpec.IsInTable(table)).ToList(),
            Join = [], 
            Where = extractedWhere.split 
        };
        
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
