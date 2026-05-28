using AlliumSativum.Optimize;
using AlliumSativum.Optimize.Interfaces;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Worker.Sdk;
using NSubstitute;

namespace QueryPlanner.Tests.OptimizerTests;

public sealed class OptimizerTestFixture
{
    public readonly ICostModel CostModel = Substitute.For<ICostModel>();
    public readonly IExpressionNodeOptimizer ExpressionNodeOptimizer = Substitute.For<IExpressionNodeOptimizer>();
    public readonly IJoinOptimizer JoinOptimizer = Substitute.For<IJoinOptimizer>();
    public readonly IPlannerApi Planner = Substitute.For<IPlannerApi>();
    public readonly ISelectOptimizer SelectOptimizer = Substitute.For<ISelectOptimizer>();
    public readonly IWhereOptimizer WhereOptimizer = Substitute.For<IWhereOptimizer>();

    public Optimizer Optimizer;

    public OptimizerTestFixture()
    {
        Optimizer = new Optimizer(
            Planner,
            ExpressionNodeOptimizer,
            JoinOptimizer,
            SelectOptimizer,
            WhereOptimizer,
            CostModel);
    }
}