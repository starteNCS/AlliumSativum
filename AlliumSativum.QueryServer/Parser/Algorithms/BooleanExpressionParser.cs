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

    public static IExpressionNode? Parse(string sqlWhere)
    {
        var tokens = Tokenize(sqlWhere);
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
                        throw new AsSqlParseException(sqlWhere, "Mismatched parentheses.");
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
                throw new AsSqlParseException(sqlWhere, "Mismatched parentheses.");
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
        
        IList<PartialColumnExpressionNode> items = [];
        while (operands.Count > 0 && items.Count != 3 && operands.Peek() is PartialColumnExpressionNode)
        {
            var topmostItem = (PartialColumnExpressionNode)operands.Pop();
            if (topmostItem.Name == AsSqlParameters.Attribute.DataSourceSeparator ||
                topmostItem.Name == AsSqlParameters.Attribute.TableSeparator.ToString())
            {
                continue;
            }
            items.Add(topmostItem);
        }

        return new FullySpecifiedColumnExpressionNode
        {
            Attribute = new AttributeSpecifier(items[2].Name, items[1].Name, items[0].Name),
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

    /// <summary>
    /// Basic Tokenizer using Regex
    /// Pattern handles:
    /// 1. Strings ('value')
    /// 2. Operators (<=, >=, !=, =, <, >)
    /// 3. Parentheses
    /// 4. Words/Numbers
    /// </summary>
    private static List<string> Tokenize(string input)
    {
        return TokenizeRegex().Matches(input)
            .Select(match => match.Value)
            .ToList();
    }

    [GeneratedRegex(@"('[^']*')|(\.|->|!=|>=|<=|=|<|>)|(\(|\))|(\w+)")]
    private static partial Regex TokenizeRegex();
}
