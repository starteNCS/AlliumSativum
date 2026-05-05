using AlliumSativum.Optimize;
using AlliumSativum.Shared.Costs;
using NSubstitute;

namespace QueryPlanner.Tests.OptimizerTests.Select;

public sealed class SelectOptimizerTestFixture
{
    public readonly ICostModel CostModel = Substitute.For<ICostModel>();

    public readonly SelectOptimizer SelectOptimizer;
    
    public SelectOptimizerTestFixture()
    {
        SelectOptimizer = new SelectOptimizer(CostModel);
    }
}
