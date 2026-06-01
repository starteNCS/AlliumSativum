using System.Text;
using AlliumSativum.Shared.Constants;

namespace AlliumSativum.Parser;

public partial class TokenQueryParser
{
    /// <summary>
    ///     Reads the stack of tokens until the next token matches a keyword, and returns the concatenated string of the read
    ///     tokens.
    /// </summary>
    /// <param name="tokens">Stack of tokens</param>
    /// <returns>Full string up until next keyword (excluding the keyword)</returns>
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

    /// <summary>
    ///     Readsthe stack of tokens until the next token matches a keyword, and returns the read tokens as a stack.
    /// </summary>
    /// <param name="tokens">Stack of tokens</param>
    /// <returns>Stack of tokens until next keyword (excluding the keyword)</returns>
    private static Stack<string> ReadTokensToNextKeyword(Stack<string> tokens)
    {
        var intermediateStack = new List<string>();

        while (tokens.TryPeek(out var token) && !AsSqlKeywords.Keywords.Contains(token))
            intermediateStack.Add(tokens.Pop());

        intermediateStack.Reverse();
        return new Stack<string>(intermediateStack);
    }
}