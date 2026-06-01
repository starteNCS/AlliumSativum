using AlliumSativum.Parser.Algorithms;
using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Parser;

public partial class TokenQueryParser
{
    /// <summary>
    ///     Reads the next tokens as a WHERE statement and updates the model accordingly
    /// </summary>
    /// <remarks>
    ///     Expects the next tokens to be in the format: "WHERE" "expression"
    /// </remarks>
    /// <param name="tokens">Stack of tokens</param>
    /// <param name="model">Current select dto</param>
    /// <exception cref="AsSqlParseException">Either one where exists or topmost token is not "WHERE"</exception>
    private void HandleWhereStatement(Stack<string> tokens, SelectDto model)
    {
        if (model.Where is not null)
            throw new AsSqlParseException("",
                $"Only one {AsSqlKeywords.WHERE} statement is allowed. Please combine them using AND");

        if (tokens.TryPeek(out var result) && result is not AsSqlKeywords.WHERE)
            throw new AsSqlParseException(result, $"Tried to parse {AsSqlKeywords.WHERE}, found {result}");
        tokens.Pop();

        var tokensOfWhere = ReadTokensToNextKeyword(tokens);
        model.Where = BooleanExpressionParser.Parse(tokensOfWhere);
    }
}