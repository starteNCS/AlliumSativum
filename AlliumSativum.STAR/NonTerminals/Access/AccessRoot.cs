using System.Diagnostics;
using AlliumSativum.STAR.NonTerminals.Join;

namespace AlliumSativum.STAR.NonTerminals.Access;

public class AccessRoot : NonTerminal
{
    public int Repo { get; }

    public AccessRoot(int repo)
    {
        Repo = repo;
    }
    
    public override HashSet<Func<NonTerminal, List<ISymbol>>> Stars { get; init; } =
    [
        (nonTerminal => AccessStar(nonTerminal as AccessRoot))
    ];

    private static List<ISymbol> AccessStar(AccessRoot? root)
    {
        Debug.Assert(root != null);
        
        return
        [
            new RepoAccess(root.Repo),
            new JoinRoot()
        ];
    }
}