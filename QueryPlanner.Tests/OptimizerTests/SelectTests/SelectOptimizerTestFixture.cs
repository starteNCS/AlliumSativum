using AlliumSativum.Optimize;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using NSubstitute;

namespace QueryPlanner.Tests.OptimizerTests.SelectTests;

public sealed class SelectOptimizerTestFixture
{
    public readonly ICostModel CostModel = Substitute.For<ICostModel>();

    public readonly SelectOptimizer SelectOptimizer;

    public SelectOptimizerTestFixture()
    {
        SelectOptimizer = new SelectOptimizer(CostModel);
    }

    public PushdownSqlPlanOperator ExamplePop =>
        new(Guid.NewGuid(), "SELECT a.type FROM algorithm a")
        {
            Self = new TableSpecifier("cs", "algorithm"),
            Width = 1
        };

    #region Mocking methods

    public void MockCalculateCost(double cost)
    {
        CostModel.CalculateCost(Arg.Any<PlanOperator>()).Returns(cost);
    }

    #endregion
}