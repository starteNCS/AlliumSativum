using System.Text.RegularExpressions;
using AlliumSativum.Parser.Constants;
using AlliumSativum.Parser.Exceptions;
using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser.Algorithms;

/// <summary>
/// Parser using the Shunting Yard Algorithm to parse given boolean expressions into a tree
/// </summary>
public static partial class BooleanExpressionParser
{
    private static bool IsOperator(string token) => OperatorPrecedence.ContainsKey(token);
    private static readonly Dictionary<string, int> OperatorPrecedence = new()
    {
        { "OR", 1 },
        { "AND", 2 },
        { "=", 3 }, { "!=", 3 }, { "<", 3 }, { ">", 3 }, { "<=", 3 }, { ">=", 3 }, { "LIKE", 3 }
    };

    public static IExpressionNode? Parse(Stack<string> tokens)
    {
        var operatorStack = new Stack<string>();
        var operandStack = new Stack<IExpressionNode>();

        foreach (var token in tokens)
        {
            if (IsOperator(token))
            {
                while (operatorStack.Count > 0 &&
                       IsOperator(operatorStack.Peek()) &&
                       OperatorPrecedence[operatorStack.Peek()] >= OperatorPrecedence[token])
                {
                    BuildNode(operatorStack, operandStack);
                }
                operatorStack.Push(token);
            }
            else switch (token)
            {
                case "(":
                    operatorStack.Push(token);
                    break;
                case ")":
                {
                    while (operatorStack.Count > 0 && operatorStack.Peek() != "(")
                    {
                        BuildNode(operatorStack, operandStack);
                    }
                
                    if(operatorStack.Count == 0)
                    {
                        throw new AsSqlParseException("", "Mismatched parentheses.");
                    }
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
            if (operatorStack.Peek() == "(")
            {
                throw new AsSqlParseException("", "Mismatched parentheses.");
            }
            BuildNode(operatorStack, operandStack);
        }

        return operandStack.Count > 0 ? operandStack.Pop() : null;
    }

    // Helper to combine top operator with top 2 operands
    private static void BuildNode(Stack<string> operators, Stack<IExpressionNode> operands)
    {
        var op = operators.Pop();
        var right = GetFullTopMostNode(operands);
        var left = GetFullTopMostNode(operands);

        operands.Push(new BinaryOperatorExpressionNode { Operation = op, Left = left, Right = right });
    }

    private static IExpressionNode GetFullTopMostNode(Stack<IExpressionNode> operands)
    {
        if (operands.Peek() is ValueExpressionNode || operands.Peek() is BinaryOperatorExpressionNode)
        {
            return operands.Pop();
        }

        if (!operands.TryPop(out var attributeName) || attributeName is not PartialColumnExpressionNode attributeNameNode)
        {
            throw new AsSqlParseException("", "Expected PartialColumnExpressionNode for attribute name");
        }

        if (!operands.TryPop(out var tableSeparator) || tableSeparator is not PartialColumnExpressionNode  tableSeparatorNode || tableSeparatorNode.Name != AsSqlParameters.Attribute.TableSeparator.ToString())
        {
            throw new AsSqlParseException("", $"Expected PartialColumnExpressionNode containing the table separator ({AsSqlParameters.Attribute.TableSeparator})");
        }

        if (!operands.TryPop(out var tableName) || tableName is not PartialColumnExpressionNode tableNameNode)
        {
            throw new AsSqlParseException("", "Expected PartialColumnExpressionNode for table name");
        }

        if (operands.Count == 0 || (operands.TryPeek(out var nextToken) && nextToken is PartialColumnExpressionNode nextTokenNode && nextTokenNode.Name != AsSqlParameters.Attribute.DataSourceSeparator))
        {
            // we've got some other item, therefore, this was an VariableMappingExpressionNode
            return new VariableMappingExpressionNode
            {
                VariableMapping = new VariableMappingSpecifier(tableNameNode.Name, attributeNameNode.Name)
            };
        }
        
        if (!operands.TryPop(out var dataSourceSeparator) || dataSourceSeparator is not PartialColumnExpressionNode
            {
                Name: AsSqlParameters.Attribute.DataSourceSeparator
            })
        {
            throw new AsSqlParseException("", $"Expected PartialColumnExpressionNode  containing the data source separator ({AsSqlParameters.Attribute.DataSourceSeparator})");
        }
        
        if (!operands.TryPop(out var dataSource) || dataSource is not PartialColumnExpressionNode dataSourceNode)
        {
            throw new AsSqlParseException("", "Expected PartialColumnExpressionNode for data source");
        }

        return new FullySpecifiedColumnExpressionNode()
        {
            Attribute = new AttributeSpecifier(dataSourceNode.Name, tableNameNode.Name, attributeNameNode.Name),
        };
    }

    private static IExpressionNode CreateOperandNode(string token)
    {
        // Simple heuristic: if it starts with single quote or is a number, it's a value.
        // Otherwise, treat as column.
        if (token.StartsWith('\'') || decimal.TryParse(token, out _))
        {
            return new ValueExpressionNode { Value = token.Trim('\'') };
        }
        return new PartialColumnExpressionNode { Name = token };
    }
}
