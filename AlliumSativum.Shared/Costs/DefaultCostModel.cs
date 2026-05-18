using AlliumSativum.Shared.Costs.Settings;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using Microsoft.Extensions.Options;

namespace AlliumSativum.Shared.Costs;

/// <summary>
///     Default cost model
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

    /// <inheritdoc/>
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

    /// <summary>
    /// Calculates the cost for a project operator
    /// </summary>
    /// <param name="project">The operator</param>
    /// <returns>The cost</returns>
    private double CalculateProjectCost(ProjectPlanOperator project)
    {
        return _settings.Project.BaseCost
               + project.ExpectedCardinality
               * (project.Attributes.Count * _settings.Project.PerAttributeCost);
    }

    /// <summary>
    /// Calculates the cost for a filter operator
    /// </summary>
    /// <param name="filter">The operator</param>
    /// <returns>The cost</returns>
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

    
    /// <summary>
    /// Calculates the cost for any join operator
    /// </summary>
    /// <param name="join">Any join POP</param>
    /// <returns>The cost</returns>
    /// <exception cref="ArgumentException">An unsupported join POP was provided</exception>
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
                + hj.Left.ExpectedCardinality * (_settings.Join.Hash.PerAttributeHashTableInitiation)
                + hj.Right.ExpectedCardinality *  (_settings.Join.Hash.PerAttributeHashTableLookup + _settings.Filter.PerAttributeCostNumeric)
                + hj.ExpectedCardinality * (hj.Right.Width * _settings.Join.Hash.PerPropertyCloneCost),
            _ => throw new ArgumentException("Unsupported join in cost calculation")
        };
    }
}