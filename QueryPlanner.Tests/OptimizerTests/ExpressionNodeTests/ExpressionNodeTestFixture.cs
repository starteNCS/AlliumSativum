using AlliumSativum.Optimize;

namespace QueryPlanner.Tests.OptimizerTests.ExpressionNodeTests;

public sealed class ExpressionNodeTestFixture
{
    public readonly ExpressionNodeOptimizer ExpressionNodeOptimizer;

    public ExpressionNodeTestFixture()
    {
        ExpressionNodeOptimizer = new ExpressionNodeOptimizer();
    }
}
