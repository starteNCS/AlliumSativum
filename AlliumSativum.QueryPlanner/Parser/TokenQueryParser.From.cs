using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Parser;

public partial class TokenQueryParser
{
    private void HandleFromStatement(Stack<string> tokens, SelectBaseModel model)
    {
        if (model.From is not null)
        {
            throw new AsSqlParseException(ReadStringToNextKeyword(tokens),
                "Only one FROM keyword is allowed in AsSQL");
        }
        
        var token = tokens.Pop();
        if (token is not AsSqlKeywords.FROM)
        {
            throw new AsSqlParseException(token, $"Could not parse FROM, as it starts with the wrong keyword (found '{token}')");
        }
        
        var fromSpecifier = GetTableSpecifier(tokens);

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
