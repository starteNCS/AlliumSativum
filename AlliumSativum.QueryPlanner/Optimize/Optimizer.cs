using AlliumSativum.Parser.Algorithms;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Shared.Utils;
using AlliumSativum.Worker.Sdk;

namespace AlliumSativum.Optimize;

public sealed partial class Optimizer
{
    private readonly PlannerApi _planner;

    public Optimizer(PlannerApi planner)
    {
        _planner = planner;
    }
    
    /// <summary>
    /// Optimizes the given SelectBaseModel into a QueryExecutionPlan
    /// Operates in multiple steps:
    /// - split the given model into TABLES ✅
    /// - check which WHERE expressions can be 100% assigned to one table ✅
    /// - check joins, merge multiple tables into one sub plan if possible ✅
    /// - check WHERE again, if any more can be pushed down ✅
    /// - propose to the worker ✅
    /// - check what it did not accept and add POP's to the plan accordingly
    /// - rule/cost-based check what POP's can be accumulated for cost reduction (if any) 
    /// - accumulate cost
    /// - return plan with cost
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="AsSqlOptimizeException"></exception>
    public async Task<QueryExecutionPlan> Optimize(SelectBaseModel model)
    {
        // split the given model into TABLES
        // check which WHERE expressions can be 100% assigned to one table
        var (onPremise, tables) = SplitIntoTables(model);

        // check joins, merge multiple tables into one sub plan if possible
        var (joinsLeftOnPremise, joinedTableSelect) = CombineTablesByJoinPushDown(onPremise.Join, tables);
        onPremise.Join = joinsLeftOnPremise;
        
        // check WHERE again, if any more can be pushed down
        AssignWhereToJoinedProposals(onPremise, joinedTableSelect);
        
        var plans = new Dictionary<List<TableSpecifier>, PlanOperator>(new ListComparer<TableSpecifier>());
        // propose to the worker
        foreach (var select in joinedTableSelect)
        {
            var (plan, unplanned) = await _planner.PlanQueryAsync(select);
            if (plan is null)
            {
                throw new AsSqlOptimizeException("Expected pushdown plan, but got none");
            }
            
            plans.Add([select.From!, ..select.Join.Select(x => x.Inner)], plan);
        }
        
        DistributeOnPremiseWhereToPlans(onPremise, joinedTableSelect, plans);
        
        if (plans.Count - 1 != onPremise.Join.Count)
        {
            throw new AsSqlOptimizeException("Cannot execute join, as the number of plans and joins mismatch");
        }

        foreach (var join in onPremise.Join)
        {
            var left = plans
                .FirstOrDefault(p => p.Key.Contains(onPremise.From!))
                .Value;
            var right = plans
                .FirstOrDefault(p => p.Key.Contains(join.Inner))
                .Value;

            var joinOperator = new JoinPlanOperator(left, right);
            plans.Remove([onPremise.From!]);
            plans.Remove([join.Inner]);
            plans.Add([onPremise.From!, join.Inner], joinOperator);
        }

        if (plans.Count != 1)
        {
            throw new AsSqlOptimizeException("Added all joins, but still multiple plans exist");
        }

        var finalPlan = plans.Single().Value;
        if (onPremise.Where is not null)
        {
            finalPlan = new WherePlanOperator(onPremise.Where)
            {
                Children = [finalPlan]
            };
        }

        return new QueryExecutionPlan()
        {
            Cost = 1,
            RootOperator = finalPlan
        };
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
        
        var extractedWhere = ExtractExpression(model.Where, table);
        
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
            Join = [], // TODO: we load each table by itself, so there are no joins for now
            Where = extractedWhere.split // TODO: first make this tree to conjunctional normal form and then only load the targetted table
        };
        
        return (selectModel, split);
    }
    
}
