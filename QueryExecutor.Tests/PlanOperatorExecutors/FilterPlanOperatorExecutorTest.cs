using AlliumSativum.QueryExecutor.PopExecutors;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using FluentAssertions;
using NUnit.Framework;
using QueryExecutor.Tests.Utils;
using QueryExecutor.Tests.Utils.Provider;
using Test.Shared.Helpers;

namespace QueryExecutor.Tests.PlanOperatorExecutors;

public sealed class FilterPlanOperatorExecutorTest
{
    [Test]
    public async Task Should_Execute_Filter_Equals_Plan_Operator()
    {
        var expression = "SELECT FROM cs->algorithm a WHERE a.id = 2".ToSelectDto().Where!;
        var pop = new FilterPlanOperator(expression)
        {
            Children = [new AlgorithmDataProvider()]
        };

        var result = await new FilterPlanOperatorExecutor().ExecuteAsync(pop);

        result.ExecutionData.Data.Should().HaveCount(1);
        var data = result.ExecutionData.Data.Single();
        data["cs->algorithm.id"].Should().Be(2);
    }
    
    [Test]
    public async Task Should_Execute_Filter_NotEquals_Plan_Operator()
    {
        var expression = "SELECT FROM cs->algorithm a WHERE a.id != 2".ToSelectDto().Where!;
        var pop = new FilterPlanOperator(expression)
        {
            Children = [new AlgorithmDataProvider()]
        };

        var result = await new FilterPlanOperatorExecutor().ExecuteAsync(pop);

        result.ExecutionData.Data.Should().HaveCount(3);
        var data = result.ExecutionData.Data;
        data.Should().NotContain(item => (int)item["cs->algorithm.id"] == 2);
    }
    
    [Test]
    public async Task Should_Execute_Filter_Greater_Plan_Operator()
    {
        var expression = "SELECT FROM cs->algorithm a WHERE a.id > 2".ToSelectDto().Where!;
        var pop = new FilterPlanOperator(expression)
        {
            Children = [new AlgorithmDataProvider()]
        };

        var result = await new FilterPlanOperatorExecutor().ExecuteAsync(pop);

        result.ExecutionData.Data.Should().HaveCount(2);
        var data = result.ExecutionData.Data;
        data.Should().Contain(item => (int)item["cs->algorithm.id"] > 2);
        
    }
    
    [Test]
    public async Task Should_Execute_Filter_GreaterEqual_Plan_Operator()
    {
        var expression = "SELECT FROM cs->algorithm a WHERE a.id >= 2".ToSelectDto().Where!;
        var pop = new FilterPlanOperator(expression)
        {
            Children = [new AlgorithmDataProvider()]
        };

        var result = await new FilterPlanOperatorExecutor().ExecuteAsync(pop);

        result.ExecutionData.Data.Should().HaveCount(3);
        var data = result.ExecutionData.Data;
        data.Should().Contain(item => (int)item["cs->algorithm.id"] >= 2);
        
    }
    
    [Test]
    public async Task Should_Execute_Filter_And_Plan_Operator()
    {
        var expression = "SELECT FROM cs->algorithm a WHERE a.id = 2 AND a.name = 'Dynamic Programming Bushy Join Tree Enumeration'".ToSelectDto().Where!;
        var pop = new FilterPlanOperator(expression)
        {
            Children = [new AlgorithmDataProvider()]
        };

        var result = await new FilterPlanOperatorExecutor().ExecuteAsync(pop);

        result.ExecutionData.Data.Should().HaveCount(1);
        var data = result.ExecutionData.Data.Single();
        data["cs->algorithm.id"].Should().Be(2);
        data["cs->algorithm.name"].Should().Be("Dynamic Programming Bushy Join Tree Enumeration");
    }
    
    [Test]
    public async Task Should_Execute_Filter_Or_Plan_Operator()
    {
        var expression = "SELECT FROM cs->algorithm a WHERE a.id = 2 OR a.id = 3".ToSelectDto().Where!;
        var pop = new FilterPlanOperator(expression)
        {
            Children = [new AlgorithmDataProvider()]
        };

        var result = await new FilterPlanOperatorExecutor().ExecuteAsync(pop);

        result.ExecutionData.Data.Should().HaveCount(2);
        var data = result.ExecutionData.Data;
        data.Should().Contain(item => (int)item["cs->algorithm.id"] == 2);
        data.Should().Contain(item => (int)item["cs->algorithm.id"] == 3);
    }
}
