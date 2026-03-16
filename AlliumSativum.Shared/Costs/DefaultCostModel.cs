using AlliumSativum.Shared.Costs.Settings;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using Microsoft.Extensions.Options;

namespace AlliumSativum.Shared.Costs;

/// <summary>
///     Default cost model, should be okay-ish for all types of data source, but connectors maybe need to implement their
///     own, to fit their specific needs
/// </summary>
public sealed partial class DefaultCostModel : ICostModel
{
    private readonly CatalogDatabase _catalog;
    private readonly CostModelSettings _settings;

    public DefaultCostModel(
        CatalogDatabase catalog,
        IOptions<CostModelSettings> settings)
    {
        _catalog = catalog;
        _settings = settings.Value;
    }

    /// <summary>
    ///     Iterates through the POP-tree and calculates the total cost of the plan, by summing up the cost of each operator
    /// </summary>
    /// <param name="planOperator"></param>
    /// <returns></returns>
    public double TotalCost(PlanOperator? planOperator, bool fromActualCost = false)
    {
        if (planOperator == null) return 0;

        // If there are no children, the cost is just this node's cost
        if (planOperator.Children.Count == 0)
            return fromActualCost
                ? planOperator.ExecutionData.ActualCost
                : planOperator.Cost;

        // Recursively find the total cost of each child's branch
        // and pick the maximum (the "more expensive" parallel path)
        var maxChildBranchCost = planOperator.Children
            .Select(child => TotalCost(child, fromActualCost))
            .Max();

        return (fromActualCost
            ? planOperator.ExecutionData.ActualCost
            : planOperator.Cost) + maxChildBranchCost;
    }

    /// <summary>
    ///     Calculates the cost of a given plan operator
    /// </summary>
    /// <remarks>
    ///     All other fields must already be initialized, as the cost of a plan operator may depend on them
    /// </remarks>
    /// <param name="op"></param>
    /// <returns>
    ///     The cost for ONLY the given plan operator
    /// </returns>
    public double CalculateCost(PlanOperator op)
    {
        return op switch
        {
            ProjectPlanOperator project => CalculateProjectCost(project),
            FilterPlanOperator filter => CalculateFilterCost(filter),
            JoinPlanOperator join => CalculateJoinCost(join),
            _ => -1
        };
    }

    private double CalculateProjectCost(ProjectPlanOperator project)
    {
        return _settings.Project.BaseCost
               + project.ExpectedCardinality
               * (project.Attributes.Count * _settings.Project.PerAttributeCost);
    }

    private double CalculateFilterCost(FilterPlanOperator filter)
    {
        var numberOfExpressions = filter.Expression.GetExpressionsCount();

        double costPerRow = 0;
        foreach (var typedCount in numberOfExpressions)
            costPerRow += typedCount.Key switch
            {
                ValueExpressionNode.ValueExpressionType.String => _settings.Filter.PerAttributeCostString,
                ValueExpressionNode.ValueExpressionType.Numeric => _settings.Filter.PerAttributeCostNumeric,
                _ => -1
            };

        return _settings.Filter.BaseCost
               + filter.ExpectedCardinality * costPerRow;
    }

    private double CalculateJoinCost(JoinPlanOperator join)
    {
        return join switch
        {
            NestedLoopJoinPlanOperator nlj =>
                _settings.Join.NestedLoop.BaseCost
                + nlj.Left.ExpectedCardinality * nlj.Right.ExpectedCardinality *
                _settings.Filter.PerAttributeCostNumeric,
            HashJoinPlanOperator hj =>
                _settings.Join.Hash.BaseCost
                + Math.Min(hj.Left.ExpectedCardinality, hj.Right.ExpectedCardinality) *
                _settings.Join.Hash.PerAttributeHashTableInitiation
                + Math.Max(hj.Left.ExpectedCardinality, hj.Right.ExpectedCardinality) *
                (_settings.Join.Hash.PerAttributeHashTableLookup + _settings.Filter.PerAttributeCostNumeric),
            MergeSortJoinPlanOperator mj => double.MaxValue,
            _ => throw new ArgumentException("Unsupported join in cost calculation")
        };
    }
}