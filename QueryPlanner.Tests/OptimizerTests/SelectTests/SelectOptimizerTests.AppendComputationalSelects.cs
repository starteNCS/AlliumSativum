using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.OptimizerTests.SelectTests;

public sealed class AppendComputationalSelectsTests
{
    private readonly SelectOptimizerTestFixture _fixture = new();
    
    [Test]
    public void Should_Throw_If_No_Select_To_Append_To()
    {
        List<AttributeSpecifier> hiddenAttributes =
        [
            new("cs", "algorithm", "id"),
            new("cs", "algorithm", "name")
        ];
        
        Action act = () => _fixture.SelectOptimizer.AppendComputationalSelects([], hiddenAttributes);

        act.ShouldThrowOptimizeException("Expected to find select model to push hidden attribute to");
    }
    
    [Test]
    public void Should_Append_To_Only_SelectDto()
    {
        var dto = "SELECT a.type FROM cs->algorithm a".ToSelectDto();
        List<AttributeSpecifier> hiddenAttributes =
        [
            new("cs", "algorithm", "id"),
            new("cs", "algorithm", "name")
        ];
        
        _fixture.SelectOptimizer.AppendComputationalSelects([dto], hiddenAttributes);

        foreach (var attribute in hiddenAttributes)
        {
            dto.Select.Should().ContainSingle(s => s is AttributeSpecifier && ((AttributeSpecifier)s).Equals(attribute) && ((AttributeSpecifier)s).IsHidden);
        }
    }
    
    [Test]
    public void Should_Skip_If_Already_Existing()
    {
        var dto = "SELECT a.type FROM cs->algorithm a".ToSelectDto();
        List<AttributeSpecifier> hiddenAttributes =
        [
            new("cs", "algorithm", "type"),
        ];
        
        _fixture.SelectOptimizer.AppendComputationalSelects([dto], hiddenAttributes);

        foreach (var attribute in hiddenAttributes)
        {
            dto.Select.Should().ContainSingle(s => s is AttributeSpecifier && ((AttributeSpecifier)s).Equals(attribute) && !((AttributeSpecifier)s).IsHidden);
        }
    }
    
    [Test]
    public void Should_Append_To_Correct_SelectDto()
    {
        var dtoAlgorithm = "SELECT a.type FROM cs->algorithm a".ToSelectDto();
        var dtoExperimentRun = "SELECT er.peak_memory_mb FROM cs->experiment_run er".ToSelectDto();
        List<AttributeSpecifier> hiddenAttributes =
        [
            new("cs", "algorithm", "id"),
            new("cs", "algorithm", "name"),
            new("cs", "experiment_run", "peak_cpu_usage_percent"),
        ];
        
        _fixture.SelectOptimizer.AppendComputationalSelects([dtoAlgorithm, dtoExperimentRun], hiddenAttributes);

        foreach (var attribute in hiddenAttributes)
        {
            switch (attribute.TableName)
            {
                case "algorithm":
                    dtoAlgorithm.Select.Should().ContainSingle(s => s is AttributeSpecifier && ((AttributeSpecifier)s).Equals(attribute) && ((AttributeSpecifier)s).IsHidden);
                    break;
                case "experiment_run":
                    dtoExperimentRun.Select.Should().ContainSingle(s => s is AttributeSpecifier && ((AttributeSpecifier)s).Equals(attribute) && ((AttributeSpecifier)s).IsHidden);
                    break;
            }
        }
    }
}
