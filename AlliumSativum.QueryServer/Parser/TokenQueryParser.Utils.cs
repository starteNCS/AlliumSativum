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
}
