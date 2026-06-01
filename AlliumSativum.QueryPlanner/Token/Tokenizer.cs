using System.Text.RegularExpressions;
using AlliumSativum.Interfaces;

namespace AlliumSativum.Token;

public partial class Tokenizer : ITokenizer
{
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public Stack<string> Tokenize(string query)
    {
        query = query.Replace(@"\r\n", " ");
        query = query.Replace(@"\n", " ");

        var tokens = JoinTokens().Matches(query)
            .Select(match => match.Value)
            .ToList();
        tokens.Reverse();
        return new Stack<string>(tokens);
    }

    [GeneratedRegex(@"('[^']*')|(,|\.|->|!=|>=|<=|=|<|>)|(\(|\))|(\w+)")]
    private static partial Regex JoinTokens();
}