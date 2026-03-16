using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser;

public partial class TokenQueryParser
{
    private void HandleSelectStatement(Stack<string> tokens, SelectBaseModel model)
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