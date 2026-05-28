using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using FluentAssertions;
using Test.Shared.Helpers;

namespace QueryPlanner.Tests.OptimizerTests.JoinTests;

public sealed class ExtractOnPremiseJoinsTests
{
    private readonly JoinOptimizerTestFixture _joinOptimizerTestFixture = new();

    [Test]
    public void Should_Not_Extract_Non_Join_Expression()
    {
        var query = """
                    SELECT a.id 
                    FROM cs->algorithm a 
                    WHERE a.name = 'test'
                    """;

        var input = query.ToSelectDto();
        var (joins, attributes) = _joinOptimizerTestFixture.JoinOptimizer.ExtractOnPremiseJoins(input);

        joins.Should().BeEmpty();
        attributes.Should().BeEmpty();
    }

    [Test]
    public void Should_Extract_Single_On_Premise_Join()
    {
        var query = """
                    SELECT a.id 
                    FROM cs->algorithm a 
                    WHERE a.name = 'test'
                        INNER JOIN shared->respondent r ON a.id = r.algorithm_id
                    """;

        var input = query.ToSelectDto();
        var (joins, attributes) = _joinOptimizerTestFixture.JoinOptimizer.ExtractOnPremiseJoins(input);

        joins.Should().HaveCount(1);
        var join = joins[0];
        join.Inner.DataSourceName.Should().Be("shared");
        join.Inner.TableName.Should().Be("respondent");

        join.Expression.Should().BeExpressionNode(new BinaryOperatorExpressionNode
        {
            Left = FullySpecifiedColumnExpressionNode.FromValues("cs", "algorithm", "id"),
            Operation = "=",
            Right = FullySpecifiedColumnExpressionNode.FromValues("shared", "respondent", "algorithm_id")
        });
    }

    [Test]
    public void Should_Extract_MultipleJoin_Single_On_Premise_Join()
    {
        var query = """
                    SELECT a.id 
                    FROM cs->algorithm a 
                    WHERE a.name = 'test'
                        INNER JOIN cs->experiment_run er ON er.algorithm_id = a.id
                        INNER JOIN shared->respondent r ON a.id = r.algorithm_id
                    """;

        var input = query.ToSelectDto();
        var (joins, attributes) = _joinOptimizerTestFixture.JoinOptimizer.ExtractOnPremiseJoins(input);

        joins.Should().HaveCount(1);
        var join = joins[0];
        join.Inner.DataSourceName.Should().Be("shared");
        join.Inner.TableName.Should().Be("respondent");

        join.Expression.Should().BeExpressionNode(new BinaryOperatorExpressionNode
        {
            Left = FullySpecifiedColumnExpressionNode.FromValues("cs", "algorithm", "id"),
            Operation = "=",
            Right = FullySpecifiedColumnExpressionNode.FromValues("shared", "respondent", "algorithm_id")
        });
    }
}