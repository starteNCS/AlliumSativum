using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace QueryPlanner.Tests.Helpers;

public static class TreeHelpers
{
    public static void HaveNodeCount(this ObjectAssertions assertions, int expectedCount)
    {
        var pop = assertions.Subject as PlanOperator;
        var actualCount = CountNodes(pop);
        actualCount.Should().Be(expectedCount);
    }

    public static void BeSemanticallyCorrect(this ObjectAssertions assertions)
    {
        var pop = assertions.Subject as PlanOperator;
        
        NodesShouldBeCorrect(pop);
    }
    
    private static int CountNodes(PlanOperator? pop)
    {
        if (pop == null)
            return 0;

        var count = 1;

        foreach (var planOperator in pop.Children)
        {
            count += CountNodes(planOperator);
        }

        return count;
    }
    
    private static void NodesShouldBeCorrect(PlanOperator? pops)
    {
        if (pops == null)
            return;

        if (pops.Children.Count == 0)
        {
            pops.Should().BeAssignableTo<PushdownPlanOperator>();
        }
        else
        {
            pops.Should().NotBeAssignableTo<PushdownPlanOperator>();
        }

        foreach (var planOperator in pops.Children)
        {
            NodesShouldBeCorrect(planOperator);
        }
    }
}
