using AlliumSativum.Optimize;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;
using NSubstitute;
using Test.Shared.Helpers;

namespace QueryPlanner.Tests.OptimizerTests;

public sealed class SplitIntoTablesTests
{
    private OptimizerTestFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = new OptimizerTestFixture();
    }

    [Test]
    public void Should_Return_Single_Datasource()
    {
        var model = "SELECT a.id FROM cs->algorithm a".ToSelectDto();
        var projections = new HashSet<AttributeSpecifier> { new("cs", "algorithm", "id") };

        var (onPremise, dataSources) = _fixture.Optimizer.SplitIntoTables(model, projections);

        dataSources.Should().HaveCount(1);
        dataSources[0].From.Should().BeEquivalentTo(new TableSpecifier("cs", "algorithm"));
    }

    [Test]
    public void Should_Return_Two_Datasources()
    {
        var model = """
                    SELECT a.id, er.id
                    FROM cs->algorithm a
                    INNER JOIN cs->experiment_run er ON er.algorithm_id = a.id
                    """.ToSelectDto();

        var projections = new HashSet<AttributeSpecifier>
        {
            new("cs", "algorithm", "id"),
            new("cs", "experiment_run", "id")
        };

        var (onPremise, dataSources) = _fixture.Optimizer.SplitIntoTables(model, projections);

        dataSources.Should().HaveCount(2);
        dataSources.Should().Contain(s => s.From.TableName == "algorithm");
        dataSources.Should().Contain(s => s.From.TableName == "experiment_run");
    }

    [Test]
    public void Should_Split_Where()
    {
        // Use real ExpressionNodeOptimizer for this test by forwarding to actual impl
        var fixture = new OptimizerTestFixture();
        fixture.ExpressionNodeOptimizer
            .ExtractExpression(Arg.Any<ExpressionNode?>(),
                Arg.Any<TableSpecifier>())
            .Returns(call =>
            {
                var realOptimizer = new ExpressionNodeOptimizer();
                return realOptimizer.ExtractExpression(
                    call.ArgAt<ExpressionNode?>(0),
                    call.ArgAt<TableSpecifier>(1));
            });

        var model = "SELECT a.id FROM cs->algorithm a WHERE a.id = 10".ToSelectDto();
        var projections = new HashSet<AttributeSpecifier> { new("cs", "algorithm", "id") };

        var (onPremise, dataSources) = fixture.Optimizer.SplitIntoTables(model, projections);

        dataSources.Should().HaveCount(1);
        dataSources[0].Where.Should().NotBeNull();
        onPremise.Where.Should().BeNull();
    }
}