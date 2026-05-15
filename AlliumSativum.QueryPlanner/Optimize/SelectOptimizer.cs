using AlliumSativum.Optimize.Interfaces;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize;

public sealed class SelectOptimizer : ISelectOptimizer
{
    private readonly ICostModel _costModel;

    public SelectOptimizer(ICostModel costModel)
    {
        _costModel = costModel;
    }

    /// <inheritdoc/>
    public List<SelectDto> AppendComputationalSelects(List<SelectDto> tableSplits, List<AttributeSpecifier> hiddenAttributes)
    {
        foreach (var attribute in hiddenAttributes)
        {
            var select = tableSplits.SingleOrDefault(s => attribute.IsInTable(s.From));
            if (select is null)
                throw new AsSqlOptimizeException("Expected to find select model to push hidden attribute to");

            if (select.Select.Any(s => s is AttributeSpecifier aSpec && aSpec.Equals(attribute)))
                // model already contains specific select
                continue;

            attribute.IsHidden = true;
            select.Select.Add(attribute);
        }

        return tableSplits;
    }

    /// <inheritdoc/>
    public PlanOperator HandleProjection(PlanOperator pop, TableSpecifier forTable, SelectDto? unplanned)
    {
        if (unplanned is null || unplanned.Select.Count == 0) return pop;

        var projected = unplanned.Select
            .OfType<AttributeSpecifier>()
            .Where(x => x.IsInTable(forTable))
            .ToList();
        unplanned.Select = unplanned.Select
            .Where(x => x is AttributeSpecifier aSpec && !aSpec.IsInTable(forTable))
            .ToList();

        var projectPop = new ProjectPlanOperator(projected)
        {
            Children = [pop],
            ExpectedCardinality = pop.ExpectedCardinality,
            Selectivity = pop.Selectivity,
            DistributionData = pop.DistributionData
                .Where(x => projected.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value),
            Width = projected.Count
        };
        projectPop.Cost = _costModel.CalculateCost(projectPop);

        return projectPop;
    }
}