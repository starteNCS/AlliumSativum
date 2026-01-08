using AlliumSativum.Exceptions;
using AlliumSativum.Parser.Algorithms;
using AlliumSativum.Parser.Constants;
using AlliumSativum.Parser.IntermediateModels;

namespace AlliumSativum.Parser;

public static partial class TokenQueryParser
{
    private static void HandleWhereStatement(Stack<string> tokens, SelectBaseModel model)
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
