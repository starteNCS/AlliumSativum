using AlliumSativum.Optimize;
using FluentAssertions;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.OptimizerTests.JoinTests;

public sealed class GetOnlyMixedJoinsTests
{
    private readonly JoinOptimizerTestFixture _fixture = new();
    
    [Test]
    public void Should_Return_Empty_List_If_NoJoins()
    {
        var query = "SELECT a.id FROM cs->algorithm a";
        
        var input = query.ToSelectDto();
        var result = JoinOptimizer.GetOnlyMixedJoins(input);
        
        result.Should().BeEmpty();
    }
    
    [Test]
    public void Should_Return_Empty_List_If_No_Mixed_Joins()
    {
        var query = """
                    SELECT a.id 
                    FROM cs->algorithm a 
                        INNER JOIN cs->experiment_run er ON er.algorithm_id = a.id
                    """;
        
        var input = query.ToSelectDto();
        var result = JoinOptimizer.GetOnlyMixedJoins(input);
        
        result.Should().BeEmpty();
    }
    
    [Test]
    public void Should_Return_Mixed_Joins()
    {
        var query = """
                    SELECT a.id 
                    FROM cs->algorithm a 
                        INNER JOIN shared->respondent r ON r.algorithm_id = a.id
                    """.ToSelectDto();
        
        var result = JoinOptimizer.GetOnlyMixedJoins(query);
        
        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(query.Join);
    }
}
