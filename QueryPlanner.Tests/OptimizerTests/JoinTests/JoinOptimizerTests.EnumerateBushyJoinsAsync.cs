using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.OptimizerTests.JoinTests;

public sealed class EnumerateBushyJoinsAsyncTests
{
    private readonly JoinOptimizerTestFixture _fixture = new();
    
    [Test]
    public async Task Should_Return_Pop_For_Single_Table()
    {
        var lookupTable = _fixture.SeedPopLookupTable([new AttributeSpecifier("cs", "algorithm", "id")]);
        
        var result = await _fixture.JoinOptimizer.EnumerateBushyJoinsAsync([], lookupTable);

        result.Should().HaveCount(1);
        result.Single().Should().Be(lookupTable.Single());
    }
    
    [Test]
    public async Task Should_Have_Semantically_Correct_Tree()
    {
        _fixture.MockRandomCost();
        _fixture.MockRandomDistribution();

        var joins = """
                    FROM cs->benchmark b
                        INNER JOIN cs->algorithm a ON a.benchmark_id = b.id
                        INNER JOIN cs->experiment_run er ON er.algorithm_id = a.id
                        INNER JOIN shared->respondent r ON r.experiment_run_id = er.id
                    """.ToSelectDto().Join;
        
        var lookupTable = _fixture.SeedPopLookupTable(
            joins.SelectMany(x => x.Expression.GetAttributesOfExpression()).ToList());
        
        var result = await _fixture.JoinOptimizer.EnumerateBushyJoinsAsync(joins, lookupTable);

        result.Should().HaveCount(1);
        var plan = result.Single();
        
        // 4 pushdown nodes + 3 join nodes
        plan.Should().HaveNodeCount(7);
        plan.Should().BeSemanticallyCorrect();
    }
}
