using AlliumSativum.Parser.Algorithms;
using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Parser;

public partial class TokenQueryParser
{
    private void HandleWhereStatement(Stack<string> tokens, SelectBaseModel model)
    {
        if (model.Where is not null)
        {
            throw new AsSqlParseException("", $"Only one {AsSqlKeywords.WHERE} statement is allowed. Please combine them using AND");
        }
        
        if (tokens.TryPeek(out var result) && result is not AsSqlKeywords.WHERE)
        {
            throw new AsSqlParseException(result, $"Tried to parse {AsSqlKeywords.WHERE}, found {result}");
        }
        tokens.Pop();
        
        var tokensOfWhere = ReadTokensToNextKeyword(tokens);
        model.Where = BooleanExpressionParser.Parse(tokensOfWhere);
    }
}
