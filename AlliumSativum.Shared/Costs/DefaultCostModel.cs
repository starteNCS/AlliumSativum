using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Costs;

/// <summary>
/// Default cost model, should be okay-ish for all types of data source, but connectors maybe need to implement their
/// own, to fit their specific needs
/// </summary>
public sealed class DefaultCostModel : ICostModel
{
    private readonly CatalogDatabase _catalog;

    public DefaultCostModel(CatalogDatabase catalog)
    {
        _catalog = catalog;
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
        return ((long) (selectivity * previousCardinality), selectivity);
    }
    
    /// <summary>
    /// Caclualtes the expected cardinality after applying a given filter
    /// </summary>
    /// <param name="join"></param>
    /// <returns></returns>
    public async Task<(long Cardinality, double Selectivity)> CalculateExpectedCardinalityAsync(JoinPlanOperator join)
    {
        var cardinality = (join.Left.ExpectedCardinality * join.Right.ExpectedCardinality) *
                          await GetSelectivityAsync((BinaryOperatorExpressionNode)join.Expression);
        return ((long) cardinality, 1);
    }
    
    /// <summary>
    /// Uses selinger style selectivity estimation, which is very basic, but should be good enough for most cases
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public async Task<double> GetSelectivityAsync(BinaryOperatorExpressionNode node)
    {
        if (node is { Left: FullySpecifiedColumnExpressionNode, Operation: "=", Right: ValueExpressionNode } or { Right: FullySpecifiedColumnExpressionNode, Operation: "=", Left: ValueExpressionNode })
        {
            return 0.1; 
        }
        
        if (node is { Left: FullySpecifiedColumnExpressionNode, Operation: ">" or "<" or ">=" or "<=", Right: ValueExpressionNode } or { Right: FullySpecifiedColumnExpressionNode, Operation: ">" or "<" or ">=" or "<=", Left: ValueExpressionNode })
        {
            var valueNode = node.Left is ValueExpressionNode left ? left : (ValueExpressionNode)node.Right;
            var attributeNode = node.Left is FullySpecifiedColumnExpressionNode leftCol ? leftCol : (FullySpecifiedColumnExpressionNode)node.Right;
            var attribute = await _catalog.GetAttributeAsync(attributeNode);
            
            if (attribute.IsNummeric && valueNode.Type == ValueExpressionNode.ValueExpressionType.Decimal)
            {
                var result = (attribute.Max - double.Parse(valueNode.Value)) / (attribute.Max - attribute.Min);
                if (result is not null)
                {
                    return result.Value;
                }
            }
            
            return 0.33; 
        }
        
        if (node is { Left: FullySpecifiedColumnExpressionNode, Operation: "=", Right: FullySpecifiedColumnExpressionNode })
        {
            return 0.1;
        }
        
        if(node is {Left : BinaryOperatorExpressionNode, Operation: "OR", Right: BinaryOperatorExpressionNode})
        {
            var leftSelectivity = await GetSelectivityAsync((BinaryOperatorExpressionNode)node.Left);
            var rightSelectivity = await GetSelectivityAsync((BinaryOperatorExpressionNode)node.Right);
            return leftSelectivity + rightSelectivity - leftSelectivity * rightSelectivity; 
        }
        
        if(node is {Left : BinaryOperatorExpressionNode, Operation: "AND", Right: BinaryOperatorExpressionNode})
        {
            var leftSelectivity = await GetSelectivityAsync((BinaryOperatorExpressionNode)node.Left);
            var rightSelectivity = await GetSelectivityAsync((BinaryOperatorExpressionNode)node.Right);
            return leftSelectivity * rightSelectivity; 
        }
        
        throw new ArgumentException("Unsupported expression node for selectivity estimation");
    }
}
