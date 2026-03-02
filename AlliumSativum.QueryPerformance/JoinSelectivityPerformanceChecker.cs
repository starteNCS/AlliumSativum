using AlliumSativum.Compiler;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

namespace AlliumSativum.QueryExecutor.Performance;

public sealed class JoinSelectivityPerformanceChecker
{
    private readonly QueryExecutor _queryExecutor;
    private readonly QueryCompiler _queryCompiler;

    public JoinSelectivityPerformanceChecker(
        QueryExecutor queryExecutor,
        QueryCompiler queryCompiler)
    {
        _queryExecutor = queryExecutor;
        _queryCompiler = queryCompiler;
    }

    public async Task<List<PerformanceCheckResult>> ExecuteJoinSelectivityPerformanceCheckerAsync()
    {
        List<string> queries =
        [
            "SELECT h.hotel_name FROM economics->hotels h INNER JOIN geography->electricity_access ea ON ea.country = h.country",
            "SELECT u.user_id FROM economics->users u INNER JOIN geography->electricity_access ea ON ea.country = u.country"
        ];

        List<PerformanceCheckResult> resultSet = [];
        foreach (var query in queries)
        {
            resultSet.Add(await CompareJoinSelectivityAsync(query));
        }

        return resultSet;
    }


    private async Task<PerformanceCheckResult> CompareJoinSelectivityAsync(string query)
    {
        var queryPlan = (await _queryCompiler.CompileAsync(query)).RootOperator;
        var joinPop = queryPlan.FindFirst<JoinPlanOperator>();
        if (joinPop is null)
        {
            throw new InvalidOperationException("No joins found in the execution plan to compare selectivity in.");
        }
        
        var result = await _queryExecutor.ExecuteAsync(queryPlan);
        
        var stack = new Stack<PlanOperator>();
        stack.Push(queryPlan);

        long? crossJoinCardinality = null;
        var leafs = 0;
        while (stack.Count > 0)
        {
            var item = stack.Pop();
            if (item.Children.Count == 0)
            {
                leafs++;
                if (leafs > 2)
                {
                    throw new InvalidOperationException("More than 2 leaf nodes in the execution plan. This method is designed for plans with exactly 2 leaf nodes.");
                }
                
                if (crossJoinCardinality == null)
                {
                    crossJoinCardinality = item.ExecutionData.ActualCardinality;
                }
                else
                {
                    crossJoinCardinality *= item.ExecutionData.ActualCardinality;
                }
            }
            else
            {
                foreach (var child in item.Children)
                {
                    stack.Push(child);
                }
            }
        }
        
        if (crossJoinCardinality == null)
        {
            throw new InvalidOperationException("No leaf nodes found in the execution plan.");
        }

        return new PerformanceCheckResult
        {
            Query = query,
            Selectivity = new PerformanceCheckResult.PerformanceCheckResultSelectivity
            {
                Expected = joinPop.Selectivity,
                Actual = joinPop.ExecutionData.ActualCardinality / (double)crossJoinCardinality.Value
            },
            Cardinality = new PerformanceCheckResult.PerformanceCheckResultCardinality
            {
                Expected = joinPop.ExpectedCardinality,
                Actual = joinPop.ExecutionData.ActualCardinality
            }
        };
    }


    public class PerformanceCheckResult
    {
        public string Query { get; set; }
        public PerformanceCheckResultSelectivity Selectivity { get; set; }
        public PerformanceCheckResultCardinality Cardinality { get; set; }

        public class PerformanceCheckResultSelectivity
        {
            public double Expected { get; set; }
            public double Actual { get; set; }
            public double Precision => Math.Abs(Expected - Actual) / Actual;
        }
        
        public class PerformanceCheckResultCardinality
        {
            public double Expected { get; set; }
            public double Actual { get; set; }
            public double Precision => Math.Abs(Expected - Actual) / Actual;
        }
    }
}

