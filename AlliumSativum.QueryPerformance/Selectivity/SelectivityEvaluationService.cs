using AlliumSativum.Shared.Models.ExecutionPlan;

namespace AlliumSativum.QueryPerformance.Selectivity;

public sealed class SelectivityEvaluationService
{
    public static List<BenchmarkSelectivityItem> EvaluateExecutedTree(PlanOperator root, bool excludeOnes, int level = 0)
    {
        List<BenchmarkSelectivityItem> result = [];
        
        var actualSelectivity = root.Children.Any() 
            ? root.ExecutionData.ActualCardinality / (double)root.Children.Select(c => c.ExecutionData?.ActualCardinality ?? throw new ArgumentException()).Aggregate((a, b) => a * b)
            : 1.0;
        if (!(actualSelectivity == 1 && root.Selectivity == 1 && excludeOnes))
        {
            result.Add(new BenchmarkSelectivityItem
            {
                Predicted = root.Selectivity,
                Actual = actualSelectivity,
                AbsoluteDiff = Math.Abs(actualSelectivity - root.Selectivity),
                Level = level
            });

        }
        level++;
        foreach (var pop in root.Children)
        {
            result.AddRange(EvaluateExecutedTree(pop, excludeOnes, level));
        }

        return result;
    }

    public class BenchmarkSelectivityItem
    {
        public double Predicted { get; set; }
        public double Actual { get; set; }
        public double AbsoluteDiff { get; set; }
        
        public int Level { get; set; }
    }
}
