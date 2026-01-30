using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize;

public partial class Optimizer
{
    /// <summary>
    /// Extracts the tree for a specific table
    /// </summary>
    /// <remarks>Tree must be in conjunctive normal form</remarks>
    /// <param name="node">The root node</param>
    /// <param name="table">The table to split for</param>
    /// <returns>
    ///     - base: the tree with the items left, that were not extracted
    ///     - split: a tree for only the provided table
    /// </returns>
    private (IExpressionNode? @base, IExpressionNode? split) ExtractExpression(IExpressionNode node,
        TableSpecifier table)
    {
        // Case 1: The current node is an AND operator.
        // We try to split both sides recursively.
        if (node is BinaryOperatorExpressionNode binary &&
            binary.Operation.Equals("AND", StringComparison.OrdinalIgnoreCase))
        {
            var (leftBase, leftSplit) = ExtractExpression(binary.Left, table);
            var (rightBase, rightSplit) = ExtractExpression(binary.Right, table);

            // Combine the splits (the parts that only reference our table)
            IExpressionNode? finalSplit;
            if (leftSplit is not null && rightSplit is not null)
            {
                finalSplit = new BinaryOperatorExpressionNode
                    { Operation = "AND", Left = leftSplit, Right = rightSplit };
            }
            else
            {
                finalSplit = leftSplit ?? rightSplit;
            }

            // Combine the remaining base (the parts that reference other tables or mixed tables)
            IExpressionNode? finalBase;
            if (leftBase is not null && rightBase is not null)
            {
                finalBase = new BinaryOperatorExpressionNode { Operation = "AND", Left = leftBase, Right = rightBase };
            }
            else
            {
                finalBase = leftBase ?? rightBase;
            }
                

            // Note: finalBase should theoretically never be null if the input wasn't empty,
            // but we return it as the @base of the tuple.
            return (finalBase, finalSplit);
        }

        // Case 2: The node is not an AND (it's either a literal comparison like OR, >, <, etc.)
        // Check if the entire subtree belongs to the table.
        if (IsPurelyTable(node, table))
        {
            return (null, node); // Entirely the target table's, so move it to 'split'
        }

        return (node, null); // Contains other tables, keep it in '@base'
    }

    private bool IsPurelyTable(IExpressionNode node, TableSpecifier table)
    {
        return node switch
        {
            ValueExpressionNode => true,

            FullySpecifiedColumnExpressionNode fully =>
                fully.Attribute.TableName == table.TableName &&
                fully.Attribute.DataSourceName == table.DataSourceName,

            BinaryOperatorExpressionNode binary =>
                IsPurelyTable(binary.Left, table) && IsPurelyTable(binary.Right, table),

            _ => false
        };
    }
}