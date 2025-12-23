using AlliumSativum.STAR.Terminals;

namespace AlliumSativum.STAR.NonTerminals;

public abstract class NonTerminal : ISymbol
{
    public abstract HashSet<Func<NonTerminal, List<ISymbol>>> Stars { get; init; }

    public void Produce()
    {
        Console.WriteLine($"Producing: {GetType().Name}");
        
        var stars = new Stack<Func<NonTerminal, List<ISymbol>>>(Stars);

        while (stars.TryPop(out var star))
        {
            var results = star(this);
            foreach (var result in results)
            {
                switch (result)
                {
                    case NonTerminal nonTerminal:
                        nonTerminal.Produce();
                        break;
                    case IPop pop:
                        Console.WriteLine(pop.GetType().Name);
                        break;
                    default:
                        Console.WriteLine("Error");
                        break;
                }
            }
        }
    }
}