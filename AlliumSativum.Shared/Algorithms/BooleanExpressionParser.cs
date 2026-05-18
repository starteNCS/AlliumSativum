using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser.Algorithms;

/// <summary>
///     Parser using the Shunting Yard Algorithm to parse given boolean expressions into a tree
/// </summary>
public static class BooleanExpressionParser
{
    private static readonly Dictionary<string, int> OperatorPrecedence = new()
    {
        { "OR", 1 },
        { "AND", 2 },
        { "=", 3 }, { "!=", 3 }, { "<", 3 }, { ">", 3 }, { "<=", 3 }, { ">=", 3 }
    };

    private static bool IsOperator(string token)
    {
        return OperatorPrecedence.ContainsKey(token);
    }

    /// <summary>
    /// Parses a list of tokens representing a boolean expression into an expression tree using the Shunting Yard Algorithm.
    /// </summary>
    /// <param name="tokens">Token stack containing only the tokens of the boolean expression</param>
    /// <returns>ExpressionNode tree</returns>
    /// <exception cref="AsSqlParseException">Some error in the boolean expression</exception>
    public static ExpressionNode? Parse(Stack<string> tokens)
    {
        var operatorStack = new Stack<string>();
        var operandStack = new Stack<ExpressionNode>();

        foreach (var token in tokens)
            if (IsOperator(token))
            {
                while (operatorStack.Count > 0 &&
                       IsOperator(operatorStack.Peek()) &&
                       OperatorPrecedence[operatorStack.Peek()] >= OperatorPrecedence[token])
                    BuildNode(operatorStack, operandStack);
                operatorStack.Push(token);
            }
            else
            {
                switch (token)
                {
                    case "(":
                        operatorStack.Push(token);
                        break;
                    case ")":
                    {
                        while (operatorStack.Count > 0 && operatorStack.Peek() != "(")
                            BuildNode(operatorStack, operandStack);

                        if (operatorStack.Count == 0) throw new AsSqlParseException("", "Mismatched parentheses.");
                        operatorStack.Pop(); // Pop the '('
                        break;
                    }
                    default:
                        // It's an operand (Column or Value)
                        operandStack.Push(CreateOperandNode(token));
                        break;
                }
            }

        // Clear remaining operators
        while (operatorStack.Count > 0)
        {
            if (operatorStack.Peek() == "(") throw new AsSqlParseException("", "Mismatched parentheses.");
            BuildNode(operatorStack, operandStack);
        }

        return operandStack.Count > 0 ? operandStack.Pop() : null;
    }

    /// <summary>
    /// Transforms any given boolean expression tree into Conjunctive Normal Form (CNF) using the distributive property of OR over AND.
    /// </summary>
    /// <remarks>
    /// Returns the same tree if it's already in CNF or cannot be transformed
    /// </remarks>
    /// <param name="node">Starting node</param>
    /// <returns>Transformed node</returns>
    public static ExpressionNode AsConjunctiveNormalForm(ExpressionNode? node)
    {
        if (node is not BinaryOperatorExpressionNode binary) return node;

        var left = AsConjunctiveNormalForm(binary.Left);
        var right = AsConjunctiveNormalForm(binary.Right);

        if (!binary.Operation.Equals("OR", StringComparison.CurrentCultureIgnoreCase))
            return new BinaryOperatorExpressionNode { Operation = binary.Operation, Left = left, Right = right };

        if (left is BinaryOperatorExpressionNode lBin && lBin.Operation.ToUpper() == "AND")
            return new BinaryOperatorExpressionNode
            {
                Operation = "AND",
                Left = AsConjunctiveNormalForm(new BinaryOperatorExpressionNode
                    { Operation = "OR", Left = lBin.Left, Right = right }),
                Right = AsConjunctiveNormalForm(new BinaryOperatorExpressionNode
                    { Operation = "OR", Left = lBin.Right, Right = right })
            };

        if (right is BinaryOperatorExpressionNode rBin && rBin.Operation.ToUpper() == "AND")
            return new BinaryOperatorExpressionNode
            {
                Operation = "AND",
                Left = AsConjunctiveNormalForm(new BinaryOperatorExpressionNode
                    { Operation = "OR", Left = left, Right = rBin.Left }),
                Right = AsConjunctiveNormalForm(new BinaryOperatorExpressionNode
                    { Operation = "OR", Left = left, Right = rBin.Right })
            };

        return new BinaryOperatorExpressionNode { Operation = binary.Operation, Left = left, Right = right };
    }

    /// <summary>
    /// Building binary operator node, that is a node with an operator and two children
    /// </summary>
    /// <param name="operators">The operator stack</param>
    /// <param name="operands">The operand stack</param>
    private static void BuildNode(Stack<string> operators, Stack<ExpressionNode> operands)
    {
        var op = operators.Pop();
        var right = GetFullTopMostNode(operands);
        var left = GetFullTopMostNode(operands);

        operands.Push(new BinaryOperatorExpressionNode { Operation = op, Left = left, Right = right });
    }

    /// <summary>
    /// Loading the top-most full node from the operand stack, which can be either a ValueExpressionNode, a BinaryOperatorExpressionNode, a FullySpecifiedColumnExpressionNode or a VariableMappingExpressionNode
    /// </summary>
    /// <param name="operands">Operand stack</param>
    /// <returns>Topmost full expression node</returns>
    /// <exception cref="AsSqlParseException">Some parsing error occured</exception>
    private static ExpressionNode GetFullTopMostNode(Stack<ExpressionNode> operands)
    {
        if (operands.Peek() is ValueExpressionNode || operands.Peek() is BinaryOperatorExpressionNode)
            return operands.Pop();

        if (!operands.TryPop(out var attributeName) ||
            attributeName is not PartialColumnExpressionNode attributeNameNode)
            throw new AsSqlParseException("", "Expected PartialColumnExpressionNode for attribute name");

        if (!operands.TryPop(out var tableSeparator) ||
            tableSeparator is not PartialColumnExpressionNode tableSeparatorNode ||
            tableSeparatorNode.Name != AsSqlParameters.Attribute.TableSeparator.ToString())
            throw new AsSqlParseException("",
                $"Expected PartialColumnExpressionNode containing the table separator ({AsSqlParameters.Attribute.TableSeparator})");

        if (!operands.TryPop(out var tableName) || tableName is not PartialColumnExpressionNode tableNameNode)
            throw new AsSqlParseException("", "Expected PartialColumnExpressionNode for table name");

        if (operands.Count == 0 || (operands.TryPeek(out var nextToken) &&
                                    nextToken is PartialColumnExpressionNode nextTokenNode &&
                                    nextTokenNode.Name != AsSqlParameters.Attribute.DataSourceSeparator)
                                || nextToken is not PartialColumnExpressionNode)
            // we've got some other item, therefore, this was an VariableMappingExpressionNode
            return new VariableMappingExpressionNode
            {
                VariableMapping = new VariableMappingSpecifier(tableNameNode.Name, attributeNameNode.Name)
            };

        if (!operands.TryPop(out var dataSourceSeparator) || dataSourceSeparator is not PartialColumnExpressionNode
            {
                Name: AsSqlParameters.Attribute.DataSourceSeparator
            })
            throw new AsSqlParseException("",
                $"Expected PartialColumnExpressionNode containing the data source separator ({AsSqlParameters.Attribute.DataSourceSeparator})");

        if (!operands.TryPop(out var dataSource) || dataSource is not PartialColumnExpressionNode dataSourceNode)
            throw new AsSqlParseException("", "Expected PartialColumnExpressionNode for data source");

        return new FullySpecifiedColumnExpressionNode
        {
            Attribute = new AttributeSpecifier(dataSourceNode.Name, tableNameNode.Name, attributeNameNode.Name)
        };
    }

    /// <summary>
    /// Load a expression node from the topmost token, which can be either a ValueExpressionNode or a PartialColumnExpressionNode, depending on the token format
    /// </summary>
    /// <remarks>
    /// Simple heuristic: if it starts with single quote or is a number, it's a value.
    /// Otherwise, treat as column.
    /// </remarks>
    /// <param name="token">The topmost token</param>
    /// <returns>Expression node</returns>
    private static ExpressionNode CreateOperandNode(string token)
    {
        var isTokenString = token.StartsWith('\'');
        var isTokenDecimal = decimal.TryParse(token, out var _);
        if (isTokenString || isTokenDecimal)
            return new ValueExpressionNode
            {
                Value = token.Trim('\''),
                Type = isTokenString
                    ? ValueExpressionNode.ValueExpressionType.String
                    : ValueExpressionNode.ValueExpressionType.Numeric
            };
        return new PartialColumnExpressionNode { Name = token };
    }
}