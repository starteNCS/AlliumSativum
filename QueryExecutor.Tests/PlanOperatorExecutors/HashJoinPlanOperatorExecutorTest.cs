using AlliumSativum.QueryExecutor.PopExecutors.Join;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Join;
using FluentAssertions;
using NUnit.Framework;
using QueryExecutor.Tests.Utils.Provider;
using Test.Shared.Helpers;

namespace QueryExecutor.Tests.PlanOperatorExecutors;

public sealed class HashJoinPlanOperatorExecutorTest
{
    [Test]
    public async Task Should_Hash_Join()
    {
        var expression = "SELECT FROM cs->experiment_run er INNER JOIN cs->algorithm a ON er.algorithm_id = a.id".ToSelectDto().Join.Single().Expression;
        var pop = new HashJoinPlanOperator(new ExperimentRunDataProviderPop(), expression, new AlgorithmDataProviderPop());

        var result = await new HashJoinPlanOperatorExecutor().ExecuteAsync(pop);

        result.ExecutionData.Data.Should().HaveCount(4);
        result.ExecutionData.Data.Should().AllSatisfy(e => e.Should()
            .ContainKey("cs->experiment_run.id")
            .And
            .ContainKey("cs->experiment_run.date")
            .And
            .ContainKey("cs->experiment_run.algorithm_id")
            .And
            .ContainKey("cs->algorithm.id")
            .And
            .ContainKey("cs->algorithm.name"));
        result.ExecutionData.Data.Should().AllSatisfy(e => e["cs->experiment_run.algorithm_id"].Should().Be(e["cs->algorithm.id"]));
    }
}
