using AlliumSativum.Optimize;

namespace QueryPlanner.Tests.Optimize.Join;

public sealed class CombineTablesByJoinPushDownTests
{
    private static readonly ExpressionNodeOptimizer ExpressionOptimizer = new();
    private static readonly JoinOptimizer JoinOptimizer = new(ExpressionOptimizer);
    
}
