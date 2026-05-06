using AlliumSativum.Optimize;
using AlliumSativum.Optimize.Interfaces;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Costs.Models;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using NSubstitute;

namespace QueryPlanner.Tests.OptimizerTests.WhereTests;

public sealed class WhereOptimizerTestFixture
{
    public readonly ICostModel CostModel = Substitute.For<ICostModel>();
    public readonly IExpressionNodeOptimizer ExpressionNodeOptimizer = Substitute.For<IExpressionNodeOptimizer>();

    public readonly WhereOptimizer WhereOptimizer;
    
    public WhereOptimizerTestFixture()
    {
        WhereOptimizer = new WhereOptimizer(ExpressionNodeOptimizer, CostModel);
    }

    #region Mocking methods

    public void MockGetDistributionOfExpressionAsync(double selectivity, long cardinality, Dictionary<AttributeSpecifier,PlanOperatorDistributionData> distribution)
    {
        CostModel
            .GetDistributionOfExpressionAsync(
                Arg.Any<BinaryOperatorExpressionNode>(),
                Arg.Any<Dictionary<AttributeSpecifier,PlanOperatorDistributionData>>(),
                Arg.Any<List<PlanOperator>>())
            .Returns(new PlanOperatorDistributionCost()
            {
                Selectivity = selectivity,
                Cardinality = cardinality,
                Distribution = distribution
            });
    }
    
    public void MockCalculateCost(double cost)
    {
        CostModel.CalculateCost(Arg.Any<PlanOperator>()).Returns(cost);
    }
    
    #endregion
    
    #region Forwarding methods 

    public void UseGetCnfSubTrees()
    {
        ExpressionNodeOptimizer.GetCnfSubTrees(Arg.Any<ExpressionNode>())
            .Returns(callInfo => new ExpressionNodeOptimizer().GetCnfSubTrees(callInfo[0] as ExpressionNode));
    }
    
    public void UseExtractExpression()
    {
        ExpressionNodeOptimizer.ExtractExpression(Arg.Any<ExpressionNode>(), Arg.Any<List<TableSpecifier>>())
            .Returns(callInfo =>
            {
                var node = callInfo[0] as ExpressionNode;
                var tables = callInfo[1] as List<TableSpecifier>;
                return new ExpressionNodeOptimizer().ExtractExpression(node, tables);
            });
    }

    public void UseMergeCnfExpressions()
    {
        ExpressionNodeOptimizer.MergeCnfExpressions(Arg.Any<ExpressionNode>(), Arg.Any<ExpressionNode>())
            .Returns(callInfo =>
            {
                var left = callInfo[0] as ExpressionNode;
                var right = callInfo[1] as ExpressionNode;
                return new ExpressionNodeOptimizer().MergeCnfExpressions(left, right);
            });
    }

    #endregion

    
}
