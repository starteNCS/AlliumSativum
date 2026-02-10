using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize;

public sealed class ExpressionNodeOptimizer
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
    public (IExpressionNode? @base, IExpressionNode? split) ExtractExpression(IExpressionNode? node,
        TableSpecifier table)
    {
        return ExtractExpression(node, [table]);
    }
    
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
    public (IExpressionNode? @base, IExpressionNode? split) ExtractExpression(IExpressionNode? node,
        List<TableSpecifier> table)
    {
        if (node is null)
        {
            return (null, null);
        }
        
        if (node is BinaryOperatorExpressionNode binary &&
            binary.Operation.Equals("AND", StringComparison.OrdinalIgnoreCase))
        {
            var (leftBase, leftSplit) = ExtractExpression(binary.Left, table);
            var (rightBase, rightSplit) = ExtractExpression(binary.Right, table);

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

            IExpressionNode? finalBase;
            if (leftBase is not null && rightBase is not null)
            {
                finalBase = new BinaryOperatorExpressionNode { Operation = "AND", Left = leftBase, Right = rightBase };
            }
            else
            {
                finalBase = leftBase ?? rightBase;
            }
                
            return (finalBase, finalSplit);
        }

        if (IsPurelyTables(node, table))
        {
            return (null, node); 
        }

        return (node, null); 
    }
    
    public bool IsPurelyTables(IExpressionNode node, List<TableSpecifier> table)
    {
        return node switch
        {
            ValueExpressionNode => true,
            FullySpecifiedColumnExpressionNode fully => table.Exists(x => x.TableName == fully.Attribute.TableName &&  x.DataSourceName == fully.Attribute.DataSourceName),
            BinaryOperatorExpressionNode binary =>
                IsPurelyTables(binary.Left, table) && IsPurelyTables(binary.Right, table),
            _ => false
        };
    }
    
    public List<AttributeSpecifier> GetAttributesOfExpression(IExpressionNode root)
    {
        var results = new HashSet<AttributeSpecifier>();
        var stack = new Stack<IExpressionNode>();
    
        stack.Push(root);

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
                    throw new AsSqlOptimizeException($"Variable mapping is not expected at this point. Should have been expanded by the semantic transformer. Did not expect alias {varMap.VariableMapping.VariableName}");
            }
        }

        return results.ToList();
    }

    public List<TableSpecifier> GetTablesOfExpression(IExpressionNode root)
    {
        return GetAttributesOfExpression(root)
            .Select(x => new TableSpecifier(x.DataSourceName, x.TableName))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Merges two expressions (which need to already be in CNF!) into a new singular expression (which also is in CNF)
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public IExpressionNode? MergeCnfExpressions(IExpressionNode? left, IExpressionNode? right)
    {
        if (left is null && right is null)
        {
            return null;
        }

        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        return new BinaryOperatorExpressionNode()
        {
            Left = left,
            Operation = "AND",
            Right = right,
        };
    }
    
    /// <summary>
    /// Get the sub-trees (all AND combined clauses) of an expression that already is in CNF
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public List<IExpressionNode> GetCnfSubTrees(IExpressionNode? root)
    {
        var clauses = new List<IExpressionNode>();
        if (root == null)
        {
            return clauses;
        }

        var stack = new Stack<IExpressionNode>();
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

    public IExpressionNode RemoveCnfExpression(IExpressionNode? fromNode, IExpressionNode remove)
    {
        var from = GetCnfSubTrees(fromNode);
        from.Remove(remove);
        return RebuildAndTree(from);
    }

    public IExpressionNode? RebuildAndTree(List<IExpressionNode> clauses)
    {
        if (clauses.Count == 0)
        {
            return null;
        }

        var root = clauses[0];
        foreach (var clause in clauses)
        {
            root = new BinaryOperatorExpressionNode
            {
                Operation = "AND",
                Left = root,
                Right = clause
            };
        }

        return root;
    }
}
