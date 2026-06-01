using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;

namespace QueryExecutor.Tests.Utils.Provider;

public sealed class ExperimentRunDataProviderPop : DataProviderPop
{
    public ExperimentRunDataProviderPop()
    {
        ExecutionData = new PlanOperatorExecutionData
        {
            Data =
            [
                new Dictionary<string, object>
                {
                    { "cs->experiment_run.id", 1 },
                    { "cs->experiment_run.date", "01-2025" },
                    { "cs->experiment_run.algorithm_id", 2 }
                },
                new Dictionary<string, object>
                {
                    { "cs->experiment_run.id", 2 },
                    { "cs->experiment_run.date", "02-2025" },
                    { "cs->experiment_run.algorithm_id", 2 }
                },
                new Dictionary<string, object>
                {
                    { "cs->experiment_run.id", 3 },
                    { "cs->experiment_run.date", "03-2025" },
                    { "cs->experiment_run.algorithm_id", 3 }
                },
                new Dictionary<string, object>
                {
                    { "cs->experiment_run.id", 4 },
                    { "cs->experiment_run.date", "04-2025" },
                    { "cs->experiment_run.algorithm_id", 4 }
                }
            ]
        };
    }
}