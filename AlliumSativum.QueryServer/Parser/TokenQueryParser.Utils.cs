using System.Text;
using AlliumSativum.Parser.Constants;

namespace AlliumSativum.Parser;

public static partial class TokenQueryParser
{
    private static string ReadStringUntilNextKeyword(Stack<string> tokens)
    {
        var builder = new StringBuilder();
        while (tokens.TryPeek(out var token) && !AsSqlKeywords.Keywords.Contains(token))
        {
            builder.Append(token);
            builder.Append(' ');
            tokens.Pop();
        }
        
        return builder.ToString();
    }

    private static Stack<string> ReadTokensToNextKeyword(Stack<string> tokens)
    {
        var intermediateStack = new List<string>();

        while (tokens.TryPeek(out var token) && !AsSqlKeywords.Keywords.Contains(token))
        {
            intermediateStack.Add(tokens.Pop());
        }
        
        // return new Stack<string>(intermediateStack.Reverse());
        intermediateStack.Reverse();
        return new  Stack<string>(intermediateStack);
    }
}
