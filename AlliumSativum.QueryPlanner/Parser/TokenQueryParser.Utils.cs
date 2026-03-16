using System.Text;
using AlliumSativum.Shared.Constants;

namespace AlliumSativum.Parser;

public partial class TokenQueryParser
{
    private static string ReadStringToNextKeyword(Stack<string> tokens)
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

        intermediateStack.Reverse();
        return new Stack<string>(intermediateStack);
    }
}