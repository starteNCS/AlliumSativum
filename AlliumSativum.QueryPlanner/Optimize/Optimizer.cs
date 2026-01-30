using AlliumSativum.Parser.Algorithms;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Worker.Sdk;

namespace AlliumSativum.Optimize;
public class ListComparer<T> : IEqualityComparer<List<T>>
{
    public bool Equals(List<T>? x, List<T>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null) return false;
        return x.SequenceEqual(y);
    }

    public int GetHashCode(List<T> obj)
    {
        // HashCode.Combine (C# 8.0+) is great for building hashes from sequences
        var hash = new HashCode();
        foreach (var item in obj)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }
}
public sealed partial class Optimizer
{
    private readonly PlannerApi _planner;

    public Optimizer(PlannerApi planner)
    {
        _planner = planner;
    }
    
    // return qexp
    public async Task<QueryExecutionPlan> Optimize(SelectBaseModel model)
    {
        var (onPremise, tables) = SplitIntoTables(model);

        var plans = new Dictionary<List<TableSpecifier>, PlanOperator>(new ListComparer<TableSpecifier>());
        foreach (var select in tables)
        {
            var plan = await _planner.PlanQueryAsync(select);
            if (plan is null)
            {
                throw new AsSqlOptimizeException("Expected pushdown plan, but got none");
            }
            
            plans.Add([select.From!], plan.RootOperator);
        }

        IExpressionNode? whereCnf = null;
        if (onPremise.Where is not null)
        {
            whereCnf = BooleanExpressionParser.AsConjunctiveNormalForm(onPremise.Where);
            
            foreach (var table in tables)
            {
                (whereCnf, var expr) = ExtractExpression(whereCnf, table.From!);

                var scanPlanOperator = plans
                    .FirstOrDefault(p => p.Key.Contains(table.From!))
                    .Value;
                if (expr is null || scanPlanOperator is null)
                {
                    continue;
                }

                var whereOperator = new WherePlanOperator(expr)
                {
                    Children = [scanPlanOperator]
                };
                
                plans[[table.From!]] = whereOperator;

                // if there are no items left in the tree, we do not need to check here further
                if (whereCnf is null)
                {
                    break;
                }
            }
        }

        if (onPremise.Join.Count == 0 && plans.Count == 1)
        {
            return new QueryExecutionPlan()
            {
                Cost = 1,
                RootOperator = plans.Single().Value
            };
        } 
        
        if (plans.Count - 1 != onPremise.Join.Count)
        {
            throw new AsSqlOptimizeException("Cannot execute join, as the number of plans and joins missmatch");
        }

        foreach (var join in model.Join)
        {
            var left = plans
                .FirstOrDefault(p => p.Key.Contains(model.From!))
                .Value;
            var right = plans
                .FirstOrDefault(p => p.Key.Contains(join.Inner))
                .Value;

            var joinOperator = new JoinPlanOperator(left, right);
            plans.Remove([model.From!]);
            plans.Remove([join.Inner]);
            plans.Add([model.From!, join.Inner], joinOperator);
        }

        if (plans.Count != 1)
        {
            throw new AsSqlOptimizeException("Added all joins, but still multiple plans exist");
        }

        var finalPlan = plans.Single().Value;
        if (whereCnf is not null)
        {
            finalPlan = new WherePlanOperator(whereCnf)
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
        var selectModel = new SelectBaseModel
        {
            From = model.From,
            Join = model.Join,
            Where = model.Where,
            Select = model.Select.Where(spec => spec is AttributeSpecifier aSpec && !aSpec.IsInTable(table)).ToList()
        };

        var split = new SelectBaseModel()
        {
            From = table,
            Select = model.Select.Where(spec => spec is AttributeSpecifier aSpec && aSpec.IsInTable(table)).ToList(),
            Join = [], // TODO: we load each table by itself, so there are no joins for now
            Where = null // TODO: first make this tree to conjunctional normal form and then only load the targetted table
        };
        
        return (selectModel, split);
    }
    
}
