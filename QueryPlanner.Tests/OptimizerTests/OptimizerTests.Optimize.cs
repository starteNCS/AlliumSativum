using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;
using NSubstitute;
using Test.Shared.Helpers;

namespace QueryPlanner.Tests.OptimizerTests;

public sealed class OptimizeTests
{
    private OptimizerTestFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = new OptimizerTestFixture();
    }

    [Test]
    public async Task Should_Optimize_SingleTable()
    {
        var model = "SELECT a.id FROM cs->algorithm a".ToSelectDto();
        var table = new TableSpecifier("cs", "algorithm");
        var attr = new AttributeSpecifier("cs", "algorithm", "id");

        var pushdownPop = new PushdownSqlPlanOperator(
            Guid.NewGuid(), "SELECT * FROM algorithm")
        {
            Self = table,
            DistributionData = [],
            ExpectedCardinality = 100,
            Width = 1,
            Cost = 5.0
        };

        var plannedItems = new SelectDto
        {
            From = table,
            Select = [attr],
            Join = []
        };

        var planContainer = new PlanContainer { Plan = pushdownPop, PlannedItems = plannedItems };

        // Setup mocks
        _fixture.JoinOptimizer
            .ExtractOnPremiseJoins(Arg.Any<SelectDto>())
            .Returns((new List<JoinBaseModel>(),
                new List<AttributeSpecifier>()));

        _fixture.SelectOptimizer
            .AppendComputationalSelects(Arg.Any<List<SelectDto>>(), Arg.Any<List<AttributeSpecifier>>())
            .Returns(call => call.ArgAt<List<SelectDto>>(0));

        _fixture.JoinOptimizer
            .CombineTableSplitsByJoinPushDown(Arg.Any<List<JoinBaseModel>>(),
                Arg.Any<List<SelectDto>>())
            .Returns(call => (new List<JoinBaseModel>(),
                call.ArgAt<List<SelectDto>>(1)));

        _fixture.Planner
            .PlanQueryAsync(Arg.Any<SelectDto>())
            .Returns(Task.FromResult<(List<PlanContainer>, SelectDto?)>(([planContainer], null)));

        _fixture.SelectOptimizer
            .HandleProjection(Arg.Any<PlanOperator>(), Arg.Any<TableSpecifier>(), Arg.Any<SelectDto?>())
            .Returns(call => call.ArgAt<PlanOperator>(0));

        _fixture.WhereOptimizer
            .DistributeWhereToProposalsAsync(Arg.Any<PlanContainer>(), Arg.Any<SelectDto>(), Arg.Any<SelectDto?>())
            .Returns(Task.FromResult<PlanOperator>(pushdownPop));

        _fixture.JoinOptimizer
            .EnumerateBushyJoinsAsync(Arg.Any<List<JoinBaseModel>>(),
                Arg.Any<PopLookupTable>(), Arg.Any<bool>())
            .Returns(Task.FromResult<List<PlanOperator>>([pushdownPop]));

        _fixture.CostModel
            .TotalCost(Arg.Any<PlanOperator>())
            .Returns(5.0);

        // Act
        var result = await _fixture.Optimizer.OptimizeAsync(model);

        // Assert
        result.Should().HaveCount(1);
        result[0].TotalCost.Should().Be(5.0);
        result[0].RootOperator.Should().Be(pushdownPop);
    }

    [Test]
    public async Task Should_Throw_No_Plan_Returned()
    {
        var model = "SELECT a.id FROM cs->algorithm a".ToSelectDto();

        _fixture.JoinOptimizer
            .ExtractOnPremiseJoins(Arg.Any<SelectDto>())
            .Returns((new List<JoinBaseModel>(),
                new List<AttributeSpecifier>()));

        _fixture.SelectOptimizer
            .AppendComputationalSelects(Arg.Any<List<SelectDto>>(), Arg.Any<List<AttributeSpecifier>>())
            .Returns(call => call.ArgAt<List<SelectDto>>(0));

        _fixture.JoinOptimizer
            .CombineTableSplitsByJoinPushDown(Arg.Any<List<JoinBaseModel>>(),
                Arg.Any<List<SelectDto>>())
            .Returns(call => (new List<JoinBaseModel>(),
                call.ArgAt<List<SelectDto>>(1)));

        // Return empty plan list to trigger exception
        _fixture.Planner
            .PlanQueryAsync(Arg.Any<SelectDto>())
            .Returns(Task.FromResult<(List<PlanContainer>, SelectDto?)>(([], null)));

        var act = async () => await _fixture.Optimizer.OptimizeAsync(model);
        await act.Should().ThrowAsync<AsSqlOptimizeException>();
    }
}