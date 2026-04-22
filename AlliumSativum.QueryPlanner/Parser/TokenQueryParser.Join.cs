using AlliumSativum.Parser.Algorithms;
using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Parser;

public partial class TokenQueryParser
{
    /// <summary>
    ///     Expects token of format INNER JOIN tableSpec varName ON expr
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="model"></param>
    /// <exception cref="AsSqlParseException"></exception>
    private void HandleJoinStatement(Stack<string> tokens, SelectBaseModel model)
    {
        if (!tokens.TryPeek(out var joinType) || !AsSqlKeywords.JoinType.Types.Contains(joinType))
            throw new AsSqlParseException($"{joinType}",
                $"Tried to parse {AsSqlKeywords.JOIN} with one of {string.Join(',', AsSqlKeywords.JoinType.Types)}, found {joinType}");
        tokens.Pop();

        if (!tokens.TryPeek(out var joinKeyword) || joinKeyword != AsSqlKeywords.JOIN)
            throw new AsSqlParseException($"{joinType} {joinKeyword}",
                $"Tried to parse {AsSqlKeywords.JOIN}, found {joinKeyword}");
        tokens.Pop();

        var tableSpecifier = GetTableSpecifier(tokens);
        if (!tokens.TryPop(out var variableName))
            throw new AsSqlParseException($"{joinType} {joinKeyword}", "Unexpected end of stream");

        if (variableName == AsSqlKeywords.ON)
            throw new AsSqlParseException($"{joinType} {joinKeyword} {variableName}",
                "Expected an variable name, not an ON yet");

        if (!tokens.TryPop(out var onKeyword) || onKeyword != AsSqlKeywords.ON)
            throw new AsSqlParseException($"{joinType} {joinKeyword}", "Expected ON expr");

        var expressionTokens = ReadTokensToNextKeyword(tokens);
        var joinExpression = BooleanExpressionParser.Parse(expressionTokens);


        model.VariableMappings.Add(new VariableMapping
        {
            Alias = variableName,
            Table = tableSpecifier
        });

        model.Join.Add(new JoinBaseModel
        {
            Expression = joinExpression,
            Inner = tableSpecifier,
            JoinType = joinType switch
            {
                AsSqlKeywords.JoinType.INNER => JoinType.Inner,
                // AsSqlKeywords.JoinType.OUTER => JoinType.Outer,
                // AsSqlKeywords.JoinType.LEFT => JoinType.Left,
                // AsSqlKeywords.JoinType.RIGHT => JoinType.Right,
                _ => throw new AsSqlParseException($"{joinType}", $"Unknown join type {joinType}")
            }
        });
    }
}