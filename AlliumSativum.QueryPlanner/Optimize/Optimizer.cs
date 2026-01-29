using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Worker.Sdk;

namespace AlliumSativum.Optimize;

public sealed class Optimizer
{
    private readonly PlannerApi _planner;

    public Optimizer(PlannerApi planner)
    {
        _planner = planner;
    }
    
    // return qexp
    public async Task<object> Optimize(SelectBaseModel model)
    {
        var (onPremise, dataSources) = SplitIntoTables(model);

        var plans = new List<QueryExecutionPlan>();
        foreach (var table in dataSources)
        {
            var plan = await _planner.PlanQueryAsync(table);
            plans.AddRange(plan);
        }

        return null!;
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
