using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.STAR.Terminals;

namespace AlliumSativum.STAR.NonTerminals;

public abstract class NonTerminal : ISymbol
{
    public abstract HashSet<Rule> Stars { get; init; }

    public void Produce()
    {
        // TODO: kosten hier mit rein nehmen
        Console.WriteLine($"Producing: {GetType().Name}");
        
        var stars = new Stack<Rule>(Stars);

        while (stars.TryPop(out var star))
        {
            var results = star.Productions(this, new SelectBaseModel());
            foreach (var result in results)
            {
                switch (result)
                {
                    case NonTerminal nonTerminal:
                        nonTerminal.Produce();
                        break;
                    case PlanOperator pop:
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

public sealed class Rule
{
    public Func<NonTerminal, SelectBaseModel, List<ISymbol>> Productions { get; set; }
    public Func<NonTerminal, SelectBaseModel, bool> Condition { get; set; }
}