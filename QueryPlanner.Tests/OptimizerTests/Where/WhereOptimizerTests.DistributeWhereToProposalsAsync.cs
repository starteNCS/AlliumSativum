using AlliumSativum.Optimize;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using FluentAssertions;
using NSubstitute;
using QueryPlanner.Tests.Helpers;

namespace QueryPlanner.Tests.OptimizerTests.Where;

public sealed class DistributeWhereToProposalsAsync
{
    private readonly WhereOptimizerTestFixture _fixture = new();

    [SetUp]
    public void Setup()
    {
        _fixture.ExpressionNodeOptimizer.ClearReceivedCalls();
    }
    
    [Test]
    public async Task Should_Return_Same_Plan_If_No_Unplanned_Where()
    {
        _fixture.UseExtractExpression();
        
        var onPremise = "SELECT FROM cs->algorithm a".ToSelectDto();

        var scanPop = new PushdownRestCallPlanOperator(Guid.NewGuid(), "GET", "https://allium-sativum.honsel.dev/cs_algorithm", null)
        {
            Self = new TableSpecifier("cs", "algorithm"),
            Width = 5
        };

        var planContainer = new PlanContainer
        {
            Plan = scanPop,
            PlannedItems = "SELECT a.peak_memory_mb FROM cs->algorithm a".ToSelectDto()
        };
        
        var pop = await _fixture.WhereOptimizer.DistributeWhereToProposalsAsync(planContainer, onPremise, null);

        pop.Should().BePop(scanPop);
     }

    [Test]
    public async Task Should_Return_Same_Plan_If_Only_OnPremise()
    {
        _fixture.UseExtractExpression();

        var attributeSpecifier = new AttributeSpecifier("cs", "algorithm", "peak_memory_mb");
        var query = "SELECT FROM cs->algorithm a WHERE a.peak_memory_mb < 20000";
        var onPremises = query.ToSelectDto();
        

        var scanPop = new PushdownRestCallPlanOperator(Guid.NewGuid(), "GET", "https://allium-sativum.honsel.dev/cs_algorithm", null)
        {
            Self = attributeSpecifier.Table,
            Width = 5
        };

        var planContainer = new PlanContainer
        {
            Plan = scanPop,
            PlannedItems = "SELECT a.peak_memory_mb FROM cs->algorithm a".ToSelectDto()
        };
        
        var pop = await _fixture.WhereOptimizer.DistributeWhereToProposalsAsync(planContainer, onPremises, new SelectBaseModel());

        pop.Should().NotBeOfType<FilterPlanOperator>();
    }
    
    [Test]
    public async Task Should_Return_Unplanned_Where()
    {
        _fixture.UseExtractExpression();
        _fixture.UseMergeCnfExpressions();
        
        var selectivity = 0.5;
        var cardinality = 1000;
        var attributeSpecifier = new AttributeSpecifier("cs", "algorithm", "peak_memory_mb");
        var distribution = new Dictionary<AttributeSpecifier, PlanOperatorDistributionData>()
        {
            {
                attributeSpecifier, new PlanOperatorDistributionData()
            }
        };
        _fixture.MockGetDistributionOfExpressionAsync(selectivity, cardinality, distribution);

        var cost = 100;
        _fixture.MockCalculateCost(cost);

        var query = "SELECT FROM cs->algorithm a WHERE a.peak_memory_mb < 20000";
        var unplanned = query.ToSelectDto();
        var expectedResultExpression = query.ToSelectDto().Where;
        

        var scanPop = new PushdownRestCallPlanOperator(Guid.NewGuid(), "GET", "https://allium-sativum.honsel.dev/cs_algorithm", null)
        {
            Self = attributeSpecifier.Table,
            Width = 5
        };

        var planContainer = new PlanContainer
        {
            Plan = scanPop,
            PlannedItems = "SELECT a.peak_memory_mb FROM cs->algorithm a".ToSelectDto()
        };
        
        var pop = await _fixture.WhereOptimizer.DistributeWhereToProposalsAsync(planContainer, new SelectBaseModel(), unplanned);

        pop.Should().BeOfType<FilterPlanOperator>();
        var filterPop = pop as FilterPlanOperator;
        filterPop.Should().NotBeNull();
        
        // should have joined both expressions
        filterPop.Expression.Should().BeExpressionNode(expectedResultExpression);

        filterPop.Selectivity.Should().Be(selectivity);
        filterPop.ExpectedCardinality.Should().Be(cardinality);
        filterPop.Cost.Should().Be(cost); 
    }
    
    [Test]
    public async Task Should_Combine_OnPremise_And_Unplanned()
    {
        _fixture.UseExtractExpression();
        _fixture.UseMergeCnfExpressions();
        
        var selectivity = 0.5;
        var cardinality = 1000;
        var attributeSpecifier = new AttributeSpecifier("cs", "algorithm", "peak_memory_mb");
        var distribution = new Dictionary<AttributeSpecifier, PlanOperatorDistributionData>()
        {
            {
                attributeSpecifier, new PlanOperatorDistributionData()
            }
        };
        _fixture.MockGetDistributionOfExpressionAsync(selectivity, cardinality, distribution);

        var cost = 100;
        _fixture.MockCalculateCost(cost);
        
        var onPremise = "SELECT FROM cs->algorithm a WHERE a.peak_memory_mb > 10000".ToSelectDto();
        var unplanned = "SELECT FROM cs->algorithm a WHERE a.peak_memory_mb < 20000".ToSelectDto();
        var expectedResultExpression =
            _fixture.ExpressionNodeOptimizer.MergeCnfExpressions(onPremise.Where, unplanned.Where);

        var scanPop = new PushdownRestCallPlanOperator(Guid.NewGuid(), "GET", "https://allium-sativum.honsel.dev/cs_algorithm", null)
        {
            Self = attributeSpecifier.Table,
            Width = 5
        };

        var planContainer = new PlanContainer
        {
            Plan = scanPop,
            PlannedItems = "SELECT a.peak_memory_mb FROM cs->algorithm a".ToSelectDto()
        };
        
        var pop = await _fixture.WhereOptimizer.DistributeWhereToProposalsAsync(planContainer, onPremise, unplanned);

        pop.Should().BeOfType<FilterPlanOperator>();
        var filterPop = pop as FilterPlanOperator;
        filterPop.Should().NotBeNull();
        
        // should have joined both expressions
        filterPop.Expression.Should().BeExpressionNode(expectedResultExpression);

        filterPop.Selectivity.Should().Be(selectivity);
        filterPop.ExpectedCardinality.Should().Be(cardinality);
        filterPop.Cost.Should().Be(cost);        
    }
    
}
