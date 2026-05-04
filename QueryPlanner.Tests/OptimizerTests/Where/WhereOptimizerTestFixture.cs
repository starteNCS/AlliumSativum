using AlliumSativum.Optimize;
using AlliumSativum.Optimize.Interfaces;
using AlliumSativum.Parser.Algorithms;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using NSubstitute;

namespace QueryPlanner.Tests.OptimizerTests.Where;

public sealed class WhereOptimizerTestFixture
{
    public readonly ICostModel CostModel = Substitute.For<ICostModel>();
    public readonly IExpressionNodeOptimizer ExpressionNodeOptimizer = Substitute.For<IExpressionNodeOptimizer>();

    public readonly WhereOptimizer WhereOptimizer;
    
    public WhereOptimizerTestFixture()
    {
        WhereOptimizer = new WhereOptimizer(ExpressionNodeOptimizer, CostModel);
    }

    /// <summary>
    /// If this method is called within a test, the "real" GetCnfSubTrees method will be used
    /// </summary>
    public void UseGetCnfSubTrees()
    {
        ExpressionNodeOptimizer.GetCnfSubTrees(Arg.Any<ExpressionNode>())
            .Returns(callInfo => new ExpressionNodeOptimizer().GetCnfSubTrees(callInfo[0] as ExpressionNode));
    }
    
}
