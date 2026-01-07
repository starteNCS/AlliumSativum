using AlliumSativum.Parser.Constants;
using AlliumSativum.Parser.IntermediateModels;

namespace AlliumSativum.Parser;

public static partial class TokenQueryParser
{
    public static SelectBaseModel? Parse(Stack<string> tokens)
    {
        var select = new SelectBaseModel();
        
        while (tokens.Count > 0)
        {
            HandleTopStatement(tokens, select);
        }
        
        
        return select;
    }
    
    private static void HandleTopStatement(Stack<string> tokens, SelectBaseModel model)
    {
        switch (tokens.Peek())
        {
            case AsSqlKeywords.SELECT:
                HandleSelectStatement(tokens, model);
                break;
            case AsSqlKeywords.FROM:
                HandleFromStatement(tokens, model);
                break;
            default:
                tokens.Pop();
                break;
        }
    }
    
}
