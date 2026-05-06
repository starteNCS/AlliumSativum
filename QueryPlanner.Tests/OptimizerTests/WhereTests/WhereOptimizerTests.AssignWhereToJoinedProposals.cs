using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using FluentAssertions;
using NSubstitute;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.OptimizerTests.WhereTests;

public class AssignWhereToJoinedProposalsTests
{
    private readonly WhereOptimizerTestFixture _fixture = new();
    
    [SetUp]
    public void Setup()
    {
        _fixture.ExpressionNodeOptimizer.ClearReceivedCalls();
    }
    
    [Test]
    public void Should_Not_Assign_Null_Input()
    {
        var model = "SELECT a.id FROM cs->algorithm a".ToSelectDto();

        _fixture.WhereOptimizer.AssignWhereToJoinedProposals(model, new List<SelectDto>());

        // test first expression AFTER guard clause
        _fixture.ExpressionNodeOptimizer
            .DidNotReceive()
            .GetCnfSubTrees(Arg.Any<ExpressionNode>());
    }
    
    [Test]
    public void Should_Not_Assign_Mixed_Clause()
    {
        var query = """
                    SELECT a.id 
                    FROM cs->algorithm a 
                        INNER JOIN cs->experiment_run er ON er.algorithm_id = a.id
                    WHERE a.name = 'test' OR er.peak_memory_mb > 10000
                    """;
        _fixture.UseGetCnfSubTrees();
        
        var input = query.ToSelectDto();
        _fixture.WhereOptimizer.AssignWhereToJoinedProposals(input, new List<SelectDto>());

        _fixture.ExpressionNodeOptimizer
            .Received(1)
            .GetCnfSubTrees(Arg.Any<ExpressionNode>());
        
        input.Should().BeSelectDto(query.ToSelectDto());
    }
    
    [Test]
    public void Should_Assign_Expression()
    {
        var query = """
                    SELECT a.id 
                    FROM cs->algorithm a 
                        INNER JOIN cs->experiment_run er ON er.algorithm_id = a.id
                    WHERE a.name = 'test'
                    """;
        
        var proposalQuery = """
                    SELECT a.id 
                    FROM cs->algorithm a 
                    """;
        _fixture.UseGetCnfSubTrees();
        
        var input = query.ToSelectDto();
        var proposal = proposalQuery.ToSelectDto();
        _fixture.WhereOptimizer.AssignWhereToJoinedProposals(input, [proposal]);

        _fixture.ExpressionNodeOptimizer
            .Received(1)
            .GetCnfSubTrees(Arg.Any<ExpressionNode>());

        var inputUntouched = query.ToSelectDto();
        input.Should().NotBeSelectDto(inputUntouched);
        input.Where.Should().BeNull();
        proposal.Where.Should().BeExpressionNode(inputUntouched.Where);
    }
}
