using AlliumSativum.Parser.Constants;
using AlliumSativum.Parser.IntermediateModels;
using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser;

public static partial class TokenQueryParser
{
    private static void HandleSelectStatement(Stack<string> tokens, SelectBaseModel model)
    {
        List<AttributeSpecifier> specifiers = [];
        do
        {
            var token = tokens.Peek();
            if (token == AsSqlKeywords.SELECT || token == ",")
            {
                tokens.Pop();
                continue;
            }

            specifiers.Add(HandleAttributeSpecifier(tokens));
        } while (!AsSqlKeywords.Keywords.Contains(tokens.Peek()));

        model.Select = specifiers;
    }
}
