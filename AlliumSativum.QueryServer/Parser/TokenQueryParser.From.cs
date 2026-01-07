using AlliumSativum.Parser.Constants;
using AlliumSativum.Parser.Exceptions;
using AlliumSativum.Parser.IntermediateModels;

namespace AlliumSativum.Parser;

public static partial class TokenQueryParser
{
    private static void HandleFromStatement(Stack<string> tokens, SelectBaseModel model)
    {
        if (model.From is not null)
        {
            throw new AsSqlParseException(ReadStringUntilNextKeyword(tokens),
                "Only one FROM keyword is allowed in AsSQL");
        }
        
        var token = tokens.Pop();
        if (token is not AsSqlKeywords.FROM)
        {
            throw new AsSqlParseException(token, $"Could not parse FROM, as it starts with the wrong keyword (found '{token}')");
        }
        
        var fromSpecifier = HandleTableSpecifier(tokens);

        if (tokens.TryPeek(out var nextToken) && !AsSqlKeywords.Keywords.Contains(nextToken))
        {
            model.VariableMappings.Add(new VariableMapping
            {
                Alias = nextToken,
                Table = fromSpecifier
            });
        }
        
        model.From = fromSpecifier;
    }
}
