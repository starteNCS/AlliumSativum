using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;

namespace QueryExecutor.Tests.Utils.Provider;

public sealed class AlgorithmDataProviderPop : DataProviderPop
{
    public AlgorithmDataProviderPop()
    {
        ExecutionData = new PlanOperatorExecutionData
        {
            Data =
            [
                new()
                {
                    { "cs->algorithm.id", 1 },
                    { "cs->algorithm.name", "Shunting-Yard" }
                },
                new()
                {
                    { "cs->algorithm.id", 2 },
                    { "cs->algorithm.name", "Dynamic Programming Bushy Join Tree Enumeration" }
                },
                new()
                {
                    { "cs->algorithm.id", 3 },
                    { "cs->algorithm.name", "Djikstra's Algorithm" }
                },
                new()
                {
                    { "cs->algorithm.id", 4 },
                    { "cs->algorithm.name", "CNF Transformation" }
                },
            ]
        };
    }
}
