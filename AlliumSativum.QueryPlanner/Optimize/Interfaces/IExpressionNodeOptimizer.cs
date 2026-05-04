using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize.Interfaces;

public interface IExpressionNodeOptimizer
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
    (ExpressionNode? @base, ExpressionNode? split) ExtractExpression(ExpressionNode? node,
        TableSpecifier table);

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
    (ExpressionNode? @base, ExpressionNode? split) ExtractExpression(ExpressionNode? node,
        List<TableSpecifier> table);

    /// <summary>
    ///     Merges two expressions (which need to already be in CNF!) into a new singular expression (which also is in CNF)
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    ExpressionNode? MergeCnfExpressions(ExpressionNode? left, ExpressionNode? right);

    /// <summary>
    ///     Get the sub-trees (all AND combined clauses) of an expression that already is in CNF
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    List<ExpressionNode> GetCnfSubTrees(ExpressionNode? root);

    ExpressionNode RemoveCnfExpression(ExpressionNode? fromNode, ExpressionNode remove);
    ExpressionNode? RebuildAndTree(List<ExpressionNode> clauses);
}
