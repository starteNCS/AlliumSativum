using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;
using NUnit.Framework;
using QueryExecutor.Tests.Utils;
using QueryExecutor.Tests.Utils.Provider;
using Test.Shared.Helpers;

namespace QueryExecutor.Tests.QueryExecutors;

public sealed class ToParallelStacks
{
    [Test]
    public void Should_Convert_Execution_Plan_To_Parallel_Stacks()
    {
        var parallelStacks = AlliumSativum.QueryExecutor.QueryExecutor.ToParallelStacks(ExampleQExP.QExP);

        parallelStacks.Continuation.Count.Should().Be(3);
        parallelStacks.Continuation.Pop().Should().BeOfType(typeof(HashJoinPlanOperator));
        parallelStacks.Continuation.Pop().Should().BeOfType(typeof(FilterPlanOperator));
        parallelStacks.Continuation.Pop().Should().BeOfType(typeof(ProjectPlanOperator));

        parallelStacks.AwaitableStacks.Count.Should().Be(2);
        parallelStacks.AwaitableStacks[0].Continuation.Count.Should().Be(1);
        parallelStacks.AwaitableStacks[0].Continuation.Pop().Should().BeOfType(typeof(ExperimentRunDataProviderPop));
        parallelStacks.AwaitableStacks[1].Continuation.Count.Should().Be(1);
        parallelStacks.AwaitableStacks[1].Continuation.Pop().Should().BeOfType(typeof(AlgorithmDataProviderPop));
    }
}
