using System.Text.RegularExpressions;

namespace AlliumSativum.Token;

public partial class Tokenizer
{
    [GeneratedRegex(@"('[^']*')|(,|\.|->|!=|>=|<=|=|<|>)|(\(|\))|(\w+)")]
    private static partial Regex JoinTokens();
    
    public Stack<string> Tokenize(string rawJoin)
    {
        var tokens = JoinTokens().Matches(rawJoin)
            .Select(match => match.Value)
            .ToList();
        tokens.Reverse();
        return new Stack<string>(tokens);
    }
}
