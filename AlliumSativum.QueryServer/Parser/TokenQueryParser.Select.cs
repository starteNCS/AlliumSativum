using AlliumSativum.Parser.Constants;
using AlliumSativum.Parser.IntermediateModels;
using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser;

public static partial class TokenQueryParser
{
    private static void HandleSelectStatement(Stack<string> tokens, SelectBaseModel model)
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
