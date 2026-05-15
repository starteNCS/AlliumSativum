using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Optimize.Interfaces;

public interface IExpressionNodeOptimizer
{
    /// <summary>
    ///     Extracts, if possible, a subtree for a specific table
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
    ///     Extracts the tree for a list of tables
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
    ///     Merges two expressions into a new singular expression (which also is in CNF)
    /// </summary>
    /// <remarks>
    /// Both expressions need to be in CNF
    /// </remarks>
    /// <param name="left">Left expression</param>
    /// <param name="right">Right expression</param>
    /// <returns>Merge of left and right, or only one of them if the other is null</returns>
    ExpressionNode? MergeCnfExpressions(ExpressionNode? left, ExpressionNode? right);

    /// <summary>
    ///     Get the sub-trees (all AND combined clauses) of an expression that already is in CNF
    /// </summary>
    /// <remarks>
    /// Root must be in CNF
    /// </remarks>
    /// <param name="root">The root of the expression to get the subtrees of</param>
    /// <returns>A list of all subtrees</returns>
    List<ExpressionNode> GetCnfSubTrees(ExpressionNode? root);

    /// <summary>
    /// Removes a specific expression from a tree
    /// </summary>
    /// 
    /// <param name="fromNode"></param>
    /// <param name="remove"></param>
    /// <returns></returns>
    ExpressionNode RemoveCnfExpression(ExpressionNode? fromNode, ExpressionNode remove);
}