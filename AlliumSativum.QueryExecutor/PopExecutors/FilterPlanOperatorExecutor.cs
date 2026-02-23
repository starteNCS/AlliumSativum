using System.Diagnostics;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.Executor;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using Microsoft.AspNetCore.Builder;

namespace AlliumSativum.QueryExecutor.PopExecutors;

public sealed class FilterPlanOperatorExecutor : IPlanOperatorExecutor<FilterPlanOperator>
{
    public Task<PlanOperator> ExecuteAsync(FilterPlanOperator pop)
    {
        var stopwatch = Stopwatch.StartNew();

        List<Dictionary<string, object>> result = [];
        foreach (var item in pop.Children.Single().ExecutionData.Data)
        {
            // TODO: nested expression
            if(pop.Expression is not BinaryOperatorExpressionNode binaryExpression)
            {
                throw new NotSupportedException("Only binary expressions are supported in filter operator for now");
            }

            if (binaryExpression is { Operation: "OR" or "AND" })
            {
                throw new NotSupportedException("Only binary expressions with comparison operators are supported in filter operator for now");
            }

            var spec = binaryExpression.Left is FullySpecifiedColumnExpressionNode col ? col : (FullySpecifiedColumnExpressionNode)binaryExpression.Right;
            var value = binaryExpression.Left is ValueExpressionNode val ? val : (ValueExpressionNode) binaryExpression.Right;
            
            if (Matches(item, spec, binaryExpression.Operation, value))
            {
                result.Add(item);
            }
        }
        
        stopwatch.Stop();
        var executionData = new PlanOperatorExecutionData
        {
            Materialized = true,
            ActualCardinality = result.Count,
            ActualCost = stopwatch.ElapsedMilliseconds,
            Data = result
        };
        pop.ExecutionData = executionData;

        return Task.FromResult<PlanOperator>(pop);
    }

    private bool Matches(Dictionary<string, object> obj, FullySpecifiedColumnExpressionNode col, string operation, ValueExpressionNode value)
    {
        if (!obj.TryGetValue(col.Attribute.AttributeName, out var attribute))
        {
            throw new AsSQLExecuteException($"Attribute not found in the object: {col.Attribute.AttributeName}");
        }
        
        switch(operation)
        {
            case "=":
                return attribute.ToString() == value.Value;
            case "!=":
                return attribute.ToString() != value.Value;
            case ">": 
                if (value.Type != ValueExpressionNode.ValueExpressionType.Numeric)
                {
                    throw new NotSupportedException("Only numeric values are supported for '>' operator");
                }
                return Convert.ToDouble(attribute) > double.Parse(value.Value);
            case ">=": 
                if (value.Type != ValueExpressionNode.ValueExpressionType.Numeric)
                {
                    throw new NotSupportedException("Only numeric values are supported for '>=' operator");
                }
                return Convert.ToDouble(attribute) >= double.Parse(value.Value);
            case "<=": 
                if (value.Type != ValueExpressionNode.ValueExpressionType.Numeric)
                {
                    throw new NotSupportedException("Only numeric values are supported for '<=' operator");
                }
                return Convert.ToDouble(attribute) <= double.Parse(value.Value);
            case "<": 
                if (value.Type != ValueExpressionNode.ValueExpressionType.Numeric)
                {
                    throw new NotSupportedException("Only numeric values are supported for '<' operator");
                }
                return Convert.ToDouble(attribute) < double.Parse(value.Value);
            default:
                return false;
        };
    }
}
