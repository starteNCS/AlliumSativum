using System.Text.Json.Serialization;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.IntermediateModels.Expressions;

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

    public int GetBooleanFactorCount()
    {
        var count = 0;
        var stack = new Stack<ExpressionNode>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            switch (current)
            {
                case BinaryOperatorExpressionNode { Operation: "AND" or "OR" } binary:
                    count++;
                    stack.Push(binary.Right);
                    stack.Push(binary.Left);
                    break;
            }
        }

        return count;
    }
    
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
    /// <returns></returns>
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

    public abstract object? ResolveValue(Dictionary<string, object> row);
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