using AlliumSativum.STAR.Terminals;

namespace AlliumSativum.STAR.NonTerminals.Join;

public class JoinRoot : NonTerminal
{
    public override HashSet<Func<NonTerminal, List<ISymbol>>> Stars { get; init; } =
    [
        (nonTerminal => assdf(nonTerminal as JoinRoot))
    ];

    private static List<ISymbol> assdf(JoinRoot? root)
    {
        return [new PushDown()];
    }
}