using System.Text.Json.Serialization;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.IntermediateModels.Expressions;

/// <summary>
/// Abstract class for all expression nodes
/// </summary>
// fix empty output for ASP.NET endpoints
[JsonPolymorphic(IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(PartialColumnExpressionNode), "partial")]
[JsonDerivedType(typeof(VariableMappingExpressionNode), "variable")]
[JsonDerivedType(typeof(FullySpecifiedColumnExpressionNode), "fullySpecified")]
[JsonDerivedType(typeof(ValueExpressionNode), "value")]
[JsonDerivedType(typeof(BinaryOperatorExpressionNode), "binary")]
public abstract class ExpressionNode
{
    public abstract string ToSqlQueryString();
    public abstract bool Equals(object? obj);
    
    /// <summary>
    /// Recursively checks if all tables in this expression are in "table"
    /// </summary>
    /// <param name="table">Tables which should only be in the expression</param>
    /// <returns>True, when all expression tables in "tables"</returns>
    public bool IsPurelyTables(List<TableSpecifier> table)
    {
        return this switch
        {
            ValueExpressionNode => true,
            FullySpecifiedColumnExpressionNode fully => table.Exists(x =>
                x.TableName == fully.Attribute.TableName && x.DataSourceName == fully.Attribute.DataSourceName),
            BinaryOperatorExpressionNode binary =>
                binary.Left.IsPurelyTables(table) && binary.Right.IsPurelyTables(table),
            _ => false
        };
    }

    /// <summary>
    /// Get all attributes used in this expression
    /// </summary>
    /// <returns>All attribtues used</returns>
    /// <exception cref="ArgumentException">A variable mapping was found</exception>
    public List<AttributeSpecifier> GetAttributesOfExpression()
    {
        var results = new HashSet<AttributeSpecifier>();
        var stack = new Stack<ExpressionNode>();

        stack.Push(this);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            switch (current)
            {
                case FullySpecifiedColumnExpressionNode fully:
                    results.Add(fully.Attribute);
                    break;

                case BinaryOperatorExpressionNode binary:
                    stack.Push(binary.Right);
                    stack.Push(binary.Left);
                    break;

                case VariableMappingExpressionNode varMap:
                    throw new ArgumentException(
                        $"Variable mapping is not expected at this point. Should have been expanded by the semantic transformer. Did not expect alias {varMap.VariableMapping.VariableName}");
            }
        }

        return results.ToList();
    }

    /// <summary>
    /// Get all tables used in this expression
    /// </summary>
    /// <returns>All tables</returns>
    public List<TableSpecifier> GetTablesOfExpression()
    {
        return GetAttributesOfExpression()
            .Select(x => new TableSpecifier(x.DataSourceName, x.TableName))
            .Distinct()
            .ToList();
    }

    /// <summary>
    ///     Returns the number of arithmetic expressions contained within this expression with the given type
    /// </summary>
    /// <returns>The number of boolean factors</returns>
    public Dictionary<ValueExpressionNode.ValueExpressionType, int> GetExpressionsCount()
    {
        var counts = new Dictionary<ValueExpressionNode.ValueExpressionType, int>();
        var stack = new Stack<ExpressionNode>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            switch (current)
            {
                case BinaryOperatorExpressionNode binary:
                    stack.Push(binary.Right);
                    stack.Push(binary.Left);
                    break;

                case ValueExpressionNode valueExpression:
                    if (counts.TryGetValue(valueExpression.Type, out var value))
                        counts[valueExpression.Type] = ++value;
                    else
                        counts[valueExpression.Type] = 1;
                    break;
            }
        }

        return counts;
    }

    /// <summary>
    /// Unpack the value of this expression for a given row of data
    /// </summary>
    /// <param name="row">The row</param>
    /// <returns>The value</returns>
    public abstract object? ResolveValue(Dictionary<string, object> row);
    
    /// <summary>
    /// Evaluate a row against this expression
    /// </summary>
    /// <param name="row">The row</param>
    /// <returns>True, if expression evaluated to true</returns>
    public abstract bool EvaluatePredicate(Dictionary<string, object> row);

    public bool IsEquiJoin()
    {
        return this is BinaryOperatorExpressionNode binary &&
               binary is
               {
                   Operation: "=", Left: FullySpecifiedColumnExpressionNode, Right: FullySpecifiedColumnExpressionNode
               };
    }
}