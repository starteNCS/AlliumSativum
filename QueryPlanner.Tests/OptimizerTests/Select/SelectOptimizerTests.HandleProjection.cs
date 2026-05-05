using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.OptimizerTests.Select;

public sealed class HandleProjectionTest
{
    private readonly SelectOptimizerTestFixture _fixture = new();

    [Test]
    public void Should_Return_Unchanged_If_No_Unplanned()
    {
        var pop = _fixture.ExamplePop;
        
        var result = _fixture.SelectOptimizer.HandleProjection(pop, pop.Self, null);
        
        result.Should().BePop(pop);
    }

    [Test]
    public void Should_Wrap_Projection_Pop()
    {
        var cost = 2345;
        _fixture.MockCalculateCost(cost);
        
        var pop = _fixture.ExamplePop;
        var unplanned = "SELECT a.name FROM cs->algorithm a".ToSelectDto();
        
        var result = _fixture.SelectOptimizer.HandleProjection(pop, pop.Self, unplanned);
        
        result.Should().NotBePop(pop);
        result.Should().BeOfType<ProjectPlanOperator>();
        var projectPop = (ProjectPlanOperator) result;
        projectPop.Children.Should().ContainSingle().Which.Should().BePop(pop);
        
        projectPop.Attributes.Should().ContainSingle(a => a.Equals(new AttributeSpecifier("cs", "algorithm", "name")));

        projectPop.Width.Should().Be(1);
        projectPop.Cost.Should().Be(cost);
        projectPop.Selectivity.Should().Be(pop.Selectivity);
        projectPop.ExpectedCardinality.Should().Be(pop.ExpectedCardinality);
    }
    
    [Test]
    public void Should_Wrap_Only_Respective_Projection_Pop()
    {
        var cost = 2345;
        _fixture.MockCalculateCost(cost);
        
        var pop = _fixture.ExamplePop;
        var unplanned = """
                        SELECT a.name, er.peak_memory_mb 
                        FROM cs->algorithm a
                            INNER JOIN cs->experiment_run er ON er.algorithm_id = a.id
                        """.ToSelectDto();
        
        var result = _fixture.SelectOptimizer.HandleProjection(pop, pop.Self, unplanned);
        
        result.Should().NotBePop(pop);
        result.Should().BeOfType<ProjectPlanOperator>();
        var projectPop = (ProjectPlanOperator) result;
        projectPop.Children.Should().ContainSingle().Which.Should().BePop(pop);
        
        projectPop.Attributes.Should().ContainSingle(a => a.Equals(new AttributeSpecifier("cs", "algorithm", "name")));
        projectPop.Attributes.Should().NotContain(a => a.Equals(new AttributeSpecifier("cs", "experiment_run", "peak_memory_mb")));
        
        projectPop.Width.Should().Be(1);
        projectPop.Cost.Should().Be(cost);
        projectPop.Selectivity.Should().Be(pop.Selectivity);
        projectPop.ExpectedCardinality.Should().Be(pop.ExpectedCardinality);
    }
}
