using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Parser;

public partial class TokenQueryParser
{
    /// <summary>
    ///     Reads the next tokens as a FROM statement and updates the model accordingly.
    /// </summary>
    /// <remarks>
    ///     Expects the next tokens to be in the format: "FROM" "dataSourceName"->"tableName" ["variableName"]
    /// </remarks>
    /// <param name="tokens">Stack of tokens</param>
    /// <param name="model">Current select dto</param>
    /// <exception cref="AsSqlParseException">Either one "FROM" was already read, or topmost token did not match</exception>
    private void HandleFromStatement(Stack<string> tokens, SelectDto model)
    {
        if (model.From is not null)
            throw new AsSqlParseException(ReadStringToNextKeyword(tokens),
                "Only one FROM keyword is allowed in AsSQL");

        var token = tokens.Pop();
        if (token is not AsSqlKeywords.FROM)
            throw new AsSqlParseException(token,
                $"Could not parse FROM, as it starts with the wrong keyword (found '{token}')");

        var fromSpecifier = GetTableSpecifier(tokens);

        if (tokens.TryPeek(out var nextToken) && !AsSqlKeywords.Keywords.Contains(nextToken))
            model.VariableMappings.Add(new VariableMapping
            {
                Alias = nextToken,
                Table = fromSpecifier
            });

        model.From = fromSpecifier;
    }
}