namespace AlliumSativum.Interfaces;

public interface ITokenizer
{
    /// <summary>
    ///     Tokenizes the given query into a stack of tokens.
    ///     The first token of the query will be on top of the stack, the last token at the bottom.
    /// </summary>
    /// <param name="query">The query in AsSQL</param>
    /// <returns>Stack of corresponding tokens</returns>
    Stack<string> Tokenize(string query);
}