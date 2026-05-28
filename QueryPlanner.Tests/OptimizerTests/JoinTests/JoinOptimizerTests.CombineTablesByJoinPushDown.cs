using FluentAssertions;
using Test.Shared.Helpers;

namespace QueryPlanner.Tests.OptimizerTests.JoinTests;

public sealed class CombineTablesByJoinPushDownTests
{
    private readonly JoinOptimizerTestFixture _fixture = new();

    [Test]
    public void Should_Return_Empty_No_Joins()
    {
        var model = "SELECT a.id FROM cs->algorithm a".ToSelectDto();

        var (joinsLeft, tableSplits) = _fixture.JoinOptimizer.CombineTableSplitsByJoinPushDown([], [model]);

        joinsLeft.Should().BeEmpty();
        tableSplits.Should().HaveCount(1);
        tableSplits[0].Should().BeSelectDto(model);
    }

    [Test]
    public void Should_Leave_Cross_Data_Source_Join()
    {
        var algorithmDto = "SELECT a.id FROM cs->algorithm a".ToSelectDto();
        var respondentDto = "SELECT r.id FROM shared->respondent r".ToSelectDto();

        var join = "FROM cs->algorithm a INNER JOIN shared->respondent r ON r.algorithm_id = a.id".ToSelectDto().Join;

        var (joinsLeft, tableSplits) =
            _fixture.JoinOptimizer.CombineTableSplitsByJoinPushDown(join, [algorithmDto, respondentDto]);

        joinsLeft.Should().HaveCount(1);
        joinsLeft[0].Should().Be(join.Single());

        tableSplits.Should().HaveCount(2);
        tableSplits.Should().Contain(x => x.Equals(respondentDto));
        tableSplits.Should().Contain(x => x.Equals(algorithmDto));
    }

    [Test]
    public void Should_Combine_Table_Splits()
    {
        var algorithmDto = "SELECT a.id FROM cs->algorithm a".ToSelectDto();
        var experimentRunDto = "SELECT er.id FROM cs->experiment_run er".ToSelectDto();

        var join = "FROM cs->algorithm a INNER JOIN cs->experiment_run er ON er.algorithm_id = a.id".ToSelectDto().Join;

        var (joinsLeft, tableSplits) =
            _fixture.JoinOptimizer.CombineTableSplitsByJoinPushDown(join, [algorithmDto, experimentRunDto]);

        joinsLeft.Should().BeEmpty();
        tableSplits.Should().HaveCount(1);
        tableSplits[0].Should()
            .BeSelectDto(
                "SELECT a.id, er.id FROM cs->algorithm a INNER JOIN cs->experiment_run er ON er.algorithm_id = a.id"
                    .ToSelectDto());
    }
}