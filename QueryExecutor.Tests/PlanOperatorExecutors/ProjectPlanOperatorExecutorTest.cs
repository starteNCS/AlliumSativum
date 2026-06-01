using AlliumSativum.QueryExecutor.PopExecutors;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;
using NUnit.Framework;
using QueryExecutor.Tests.Utils.Provider;

namespace QueryExecutor.Tests.PlanOperatorExecutors;

public sealed class ProjectPlanOperatorExecutorTest
{
    [Test]
    public async Task Should_Project_Plan_Operator()
    {
        var pop = new ProjectPlanOperator(new AttributeSpecifier("cs", "algorithm", "id"))
        {
            Children = [new AlgorithmDataProviderPop()]
        };

        var result = await new ProjectPlanOperatorExecutor().ExecuteAsync(pop);

        result.ExecutionData.Data.Should().HaveCount(4);
        result.ExecutionData.Data.Should().AllSatisfy(e => e.Should().ContainKey("cs->algorithm.id"));
        result.ExecutionData.Data.Should().AllSatisfy(e => e.Should().NotContainKey("cs->algorithm.name"));
    }
}