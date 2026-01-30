using System.Text.RegularExpressions;

namespace AlliumSativum.Token;

public partial class Tokenizer
{
    [GeneratedRegex(@"('[^']*')|(,|\.|->|!=|>=|<=|=|<|>)|(\(|\))|(\w+)")]
    private static partial Regex JoinTokens();
    
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
}
