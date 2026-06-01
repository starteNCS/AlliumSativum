using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using QueryExecutor.Tests.Utils.Provider;

namespace QueryExecutor.Tests.Utils;

public sealed class ExampleQExP
{
    public static PlanOperator QExP => new ProjectPlanOperator(new AttributeSpecifier("cs", "algorithm", "name"))
    {
        Children =
        [
            new FilterPlanOperator(new BinaryOperatorExpressionNode
            {
                Left = FullySpecifiedColumnExpressionNode.FromValues("cs", "algorithm", "id"),
                Operation = "=",
                Right = ValueExpressionNode.FromValues(ValueExpressionNode.ValueExpressionType.Numeric, "1")
            })
            {
                Children =
                [
                    new HashJoinPlanOperator(
                        new ExperimentRunDataProviderPop(),
                        new BinaryOperatorExpressionNode
                        {
                            Left = FullySpecifiedColumnExpressionNode.FromValues("cs", "algorithm", "id"),
                            Operation = "=",
                            Right = FullySpecifiedColumnExpressionNode.FromValues("cs", "experiment_run",
                                "algorithm_id")
                        },
                        new AlgorithmDataProviderPop())
                ]
            }
        ]
    };
}