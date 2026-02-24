using AlliumSativum.Shared.Costs.Settings;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using Microsoft.Extensions.Options;

namespace AlliumSativum.Shared.Costs;

/// <summary>
/// Default cost model, should be okay-ish for all types of data source, but connectors maybe need to implement their
/// own, to fit their specific needs
/// </summary>
public sealed class DefaultCostModel : ICostModel
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
    /// Iterates through the POP-tree and calculates the total cost of the plan, by summing up the cost of each operator
    /// </summary>
    /// <param name="planOperator"></param>
    /// <returns></returns>
    public double TotalCost(PlanOperator planOperator)
    {
        // todo: only max of both child branches, not the sum, as they can be executed in parallel
        var stack = new Stack<PlanOperator>();
        stack.Push(planOperator);
        double totalCost = 0;

        while (stack.Count > 0)
        {
            var item = stack.Pop();
            totalCost += item.Cost;

            foreach (var child in item.Children)
            {
                stack.Push(child);
            }
        }

        return totalCost;
    }

    /// <summary>
    /// Calculates the cost of a given plan operator
    /// </summary>
    /// <remarks>
    /// All other fields must already be initialized, as the cost of a plan operator may depend on them
    /// </remarks>
    /// <param name="op"></param>
    /// <returns>
    /// The cost for ONLY the given plan operator
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
    /// Caclualtes the expected cardinality after applying a given filter
    /// </summary>
    /// <param name="node"></param>
    /// <param name="previousCardinality"></param>
    /// <returns></returns>
    public async Task<(long Cardinality, double Selectivity)> CalculateExpectedCardinalityAsync(BinaryOperatorExpressionNode node, long previousCardinality)
    {
        var selectivity = await GetSelectivityAsync(node);
        var cardinality = Math.Max(1, (long)(selectivity * previousCardinality));
        return (cardinality, selectivity);
    }
    
    /// <summary>
    /// Caclualtes the expected cardinality after applying a given filter
    /// </summary>
    /// <param name="join"></param>
    /// <returns></returns>
    public async Task<(long Cardinality, double Selectivity)> CalculateExpectedCardinalityAsync(JoinPlanOperator join)
    {
        var selectivity = await GetSelectivityAsync((BinaryOperatorExpressionNode)join.Expression);
        var cardinality = Math.Max(1.0, (join.Left.ExpectedCardinality * join.Right.ExpectedCardinality) *
                          selectivity);
        return ((long) cardinality, selectivity);
    }
    
    /// <summary>
    /// Uses selinger style selectivity estimation, which is very basic, but should be good enough for most cases
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public async Task<double> GetSelectivityAsync(BinaryOperatorExpressionNode node)
    {
        switch (node)
        {
            case { Left: FullySpecifiedColumnExpressionNode, Operation: "=", Right: ValueExpressionNode } or { Right: FullySpecifiedColumnExpressionNode, Operation: "=", Left: ValueExpressionNode }:
                var attr = await _catalog.GetAttributeAsync((FullySpecifiedColumnExpressionNode)node.Left);
                var ratio = 1.0 / attr.DistinctCardinality;
                
                return ratio;
            case { Left: FullySpecifiedColumnExpressionNode, Operation: ">" or "<" or ">=" or "<=", Right: ValueExpressionNode } or { Right: FullySpecifiedColumnExpressionNode, Operation: ">" or "<" or ">=" or "<=", Left: ValueExpressionNode }:
            {
                var valueNode = node.Left as ValueExpressionNode ?? (ValueExpressionNode)node.Right;
                var attributeNode = node.Left as FullySpecifiedColumnExpressionNode ?? (FullySpecifiedColumnExpressionNode)node.Right;
                var attribute = await _catalog.GetAttributeAsync(attributeNode);
            
                if (attribute.IsNummeric && valueNode.Type == ValueExpressionNode.ValueExpressionType.Numeric)
                {
                    var result = (attribute.Max - double.Parse(valueNode.Value)) / (attribute.Max - attribute.Min);
                    if (result is not null)
                    {
                        return result.Value;
                    }
                }
            
                return 0.33;
            }
            
            case { Left: FullySpecifiedColumnExpressionNode, Operation: ">" or "<" or ">=" or "<=", Right: FullySpecifiedColumnExpressionNode }:
            {
                // TODO: selinger has not proposed this case - therefore we need to do some here
                return 0.5;
            }
            case { Left: FullySpecifiedColumnExpressionNode, Operation: "=", Right: FullySpecifiedColumnExpressionNode }:
                var leftAttribute = await _catalog.GetAttributeAsync((FullySpecifiedColumnExpressionNode)node.Left);
                var leftRelation = await _catalog.GetRelationAsync(leftAttribute.RelationId);
                var rightAttribute = await _catalog.GetAttributeAsync((FullySpecifiedColumnExpressionNode)node.Right);
                var rightRelation = await _catalog.GetRelationAsync(rightAttribute.RelationId);

                // If the cardinality of the relation is the same as the distinct cardinality of the attribute,
                // we can assume that the attribute is a unique key
                // therefore each value will appear once. If both attributes are unique keys, we use the normal formular
                if((leftAttribute.DistinctCardinality == leftRelation.Cardinality &&
                    rightAttribute.DistinctCardinality == rightRelation.Cardinality) || 
                   (leftAttribute.DistinctCardinality != leftRelation.Cardinality &&
                    rightAttribute.DistinctCardinality != rightRelation.Cardinality))
                {
                    return 1.0 / Math.Min(leftAttribute.DistinctCardinality, rightAttribute.DistinctCardinality);
                }
                
                if (leftAttribute.DistinctCardinality == leftRelation.Cardinality)
                {
                    return 1.0 / leftAttribute.DistinctCardinality;
                } 
                if (rightAttribute.DistinctCardinality == rightRelation.Cardinality)
                {
                    return 1.0 / rightAttribute.DistinctCardinality;
                }
                
                return -1;
            case {Left : BinaryOperatorExpressionNode, Operation: "OR", Right: BinaryOperatorExpressionNode}:
            {
                var leftSelectivity = await GetSelectivityAsync((BinaryOperatorExpressionNode)node.Left);
                var rightSelectivity = await GetSelectivityAsync((BinaryOperatorExpressionNode)node.Right);
                return leftSelectivity + rightSelectivity - leftSelectivity * rightSelectivity;
            }
            case {Left : BinaryOperatorExpressionNode, Operation: "AND", Right: BinaryOperatorExpressionNode}:
            {
                var leftSelectivity = await GetSelectivityAsync((BinaryOperatorExpressionNode)node.Left);
                var rightSelectivity = await GetSelectivityAsync((BinaryOperatorExpressionNode)node.Right);
                return leftSelectivity * rightSelectivity;
            }
            default:
                throw new ArgumentException("Unsupported expression node for selectivity estimation");
        }
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
        {
            costPerRow += typedCount.Key switch
            {
                ValueExpressionNode.ValueExpressionType.String => _settings.Filter.PerAttributeCostString,
                ValueExpressionNode.ValueExpressionType.Numeric => _settings.Filter.PerAttributeCostNumeric,
                _ => -1
            };
        }
        
        return _settings.Filter.BaseCost 
               + filter.ExpectedCardinality * costPerRow;
    }
    
    private double CalculateJoinCost(JoinPlanOperator join)
    {
        // For simplicity, we assume a nested loop join, which has a cost of O(n*m), where n and m are the cardinalities of the left and right branches
        return 0.5 
               + (join.Left.ExpectedCardinality * join.Right.ExpectedCardinality) * 0.001;
    }
}
