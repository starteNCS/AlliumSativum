using AlliumSativum.Optimize;
using AlliumSativum.Optimize.Interfaces;
using AlliumSativum.Shared.Costs;
using NSubstitute;

namespace QueryPlanner.Tests.OptimizerTests.JoinTests;

public sealed class JoinOptimizerTestFixture
{
    public readonly ICostModel CostModel = Substitute.For<ICostModel>();
    public readonly IExpressionNodeOptimizer ExpressionNodeOptimizer = Substitute.For<IExpressionNodeOptimizer>();

    public readonly JoinOptimizer JoinOptimizer;
    
    public JoinOptimizerTestFixture()
    {
        JoinOptimizer = new JoinOptimizer(ExpressionNodeOptimizer, CostModel);
    }
}
