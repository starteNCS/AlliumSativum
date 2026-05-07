using AlliumSativum.Optimize.Interfaces;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize;

public sealed class ExpressionNodeOptimizer : IExpressionNodeOptimizer
{
    /// <summary>
    ///     Extracts the tree for a specific table
    /// </summary>
    /// <remarks>Tree must be in conjunctive normal form</remarks>
    /// <param name="node">The root node</param>
    /// <param name="table">The table to split for</param>
    /// <returns>
    ///     - base: the tree with the items left, that were not extracted
    ///     - split: a tree for only the provided table
    /// </returns>
    public (ExpressionNode? @base, ExpressionNode? split) ExtractExpression(ExpressionNode? node,
        TableSpecifier table)
    {
        return ExtractExpression(node, [table]);
    }

    /// <summary>
    ///     Extracts the tree for a specific table
    /// </summary>
    /// <remarks>Tree must be in conjunctive normal form</remarks>
    /// <param name="node">The root node</param>
    /// <param name="table">The table to split for</param>
    /// <returns>
    ///     - base: the tree with the items left, that were not extracted
    ///     - split: a tree for only the provided table
    /// </returns>
    public (ExpressionNode? @base, ExpressionNode? split) ExtractExpression(ExpressionNode? node,
        List<TableSpecifier> table)
    {
        if (node is null) return (null, null);

        if (node is BinaryOperatorExpressionNode binary &&
            binary.Operation.Equals("AND", StringComparison.OrdinalIgnoreCase))
        {
            var (leftBase, leftSplit) = ExtractExpression(binary.Left, table);
            var (rightBase, rightSplit) = ExtractExpression(binary.Right, table);

            ExpressionNode? finalSplit;
            if (leftSplit is not null && rightSplit is not null)
                finalSplit = new BinaryOperatorExpressionNode
                    { Operation = "AND", Left = leftSplit, Right = rightSplit };
            else
                finalSplit = leftSplit ?? rightSplit;

            ExpressionNode? finalBase;
            if (leftBase is not null && rightBase is not null)
                finalBase = new BinaryOperatorExpressionNode { Operation = "AND", Left = leftBase, Right = rightBase };
            else
                finalBase = leftBase ?? rightBase;

            return (finalBase, finalSplit);
        }

        if (node.IsPurelyTables(table)) return (null, node);

        return (node, null);
    }


    /// <summary>
    ///     Merges two expressions (which need to already be in CNF!) into a new singular expression (which also is in CNF)
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public ExpressionNode? MergeCnfExpressions(ExpressionNode? left, ExpressionNode? right)
    {
        if (left is null && right is null) return null;

        if (left is null) return right;

        if (right is null) return left;

        return new BinaryOperatorExpressionNode
        {
            Left = left,
            Operation = "AND",
            Right = right
        };
    }

    /// <summary>
    ///     Get the sub-trees (all AND combined clauses) of an expression that already is in CNF
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public List<ExpressionNode> GetCnfSubTrees(ExpressionNode? root)
    {
        var clauses = new List<ExpressionNode>();
        if (root == null) return clauses;

        var stack = new Stack<ExpressionNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current is BinaryOperatorExpressionNode binary &&
                binary.Operation.Equals("AND", StringComparison.OrdinalIgnoreCase))
            {
                stack.Push(binary.Right);
                stack.Push(binary.Left);
            }
            else
            {
                clauses.Add(current);
            }
        }

        return clauses;
    }

    public ExpressionNode RemoveCnfExpression(ExpressionNode? fromNode, ExpressionNode remove)
    {
        var from = GetCnfSubTrees(fromNode);
        from.Remove(remove);
        return RebuildAndTree(from);
    }

    private ExpressionNode? RebuildAndTree(List<ExpressionNode> clauses)
    {
        if (clauses.Count == 0) return null;
        if(clauses.Count == 1) return clauses[0];
        
        var root = clauses[0];
        foreach (var clause in clauses.Skip(1))
            root = new BinaryOperatorExpressionNode
            {
                Operation = "AND",
                Left = root,
                Right = clause
            };

        return root;
    }
}