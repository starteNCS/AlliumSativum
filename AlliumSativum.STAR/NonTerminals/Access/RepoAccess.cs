using System.Diagnostics;
using AlliumSativum.STAR.Terminals;

namespace AlliumSativum.STAR.NonTerminals.Access;

public class RepoAccess : NonTerminal
{
    public RepoAccess(int repo)
    {
        
    }
    
    public override HashSet<Func<NonTerminal, List<ISymbol>>> Stars { get; init; } =
    [
        (nonTerminal => PushDownStar(nonTerminal as RepoAccess))
    ];
    
    private static List<ISymbol> PushDownStar(RepoAccess? nonTerminal)
    {
        Debug.Assert(nonTerminal != null);
        
        return
        [
            new PushDown()
        ];
    }
}