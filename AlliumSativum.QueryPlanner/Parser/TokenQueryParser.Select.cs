using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser;

public partial class TokenQueryParser
{
    /// <summary>
    ///     Reads the next tokens as a SELECT statement and updates the model accordingly.
    /// </summary>
    /// <remarks>
    ///     Expects the next tokens to be in the format: "SELECT" "specifier" ("," "specifier")*
    /// </remarks>
    /// <param name="tokens">Stack oftokens</param>
    /// <param name="model">The current select DTO</param>
    private void HandleSelectStatement(Stack<string> tokens, SelectDto model)
    {
        List<ISpecifier> specifiers = [];
        do
        {
            var token = tokens.Peek();
            if (token is AsSqlKeywords.SELECT or ",")
            {
                tokens.Pop();
                continue;
            }

            specifiers.Add(GetVariableOrAttributeSpecifier(tokens));
        } while (!AsSqlKeywords.Keywords.Contains(tokens.Peek()));

        model.Select = specifiers;
    }
}