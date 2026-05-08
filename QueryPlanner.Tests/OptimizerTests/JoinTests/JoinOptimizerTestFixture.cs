using AlliumSativum.Optimize;
using AlliumSativum.Optimize.Interfaces;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Costs.Models;
using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using AlliumSativum.Shared.Utils;
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

    #region Mock methods

    public void MockRandomCost()
    {
        CostModel.CalculateCost(Arg.Any<PlanOperator>()).Returns(callInfo => new Random().NextDouble() * 100);
    }

    public void MockRandomDistribution()
    {
        CostModel
            .GetDistributionOfExpressionAsync(
                Arg.Any<BinaryOperatorExpressionNode>(),
                Arg.Any<Dictionary<AttributeSpecifier, PlanOperatorDistributionData>>(),
                Arg.Any<List<PlanOperator>>())
            .Returns(callArgs =>
            {
                var node = (BinaryOperatorExpressionNode)callArgs[0];
                var attributesOfExpression = node.GetAttributesOfExpression();

                var attributeDistributionData = new Dictionary<AttributeSpecifier, PlanOperatorDistributionData>();
                foreach (var table in attributesOfExpression.Select(a => a.Table).Distinct())
                {
                    var random = new Random();
                    foreach (var attribute in attributesOfExpression.Where(a => a.IsInTable(table)))
                    {
                        attributeDistributionData.Add(attribute, new PlanOperatorDistributionData
                        {
                            DistributionType = DistributionType.Constant,
                            Max = random.Next(50, 100),
                            Min = random.Next(0, 10),
                            Mean = random.Next(10, 20),
                            MeanBinHeight = random.NextDouble() * 10 + 10,
                            Peaks = []
                        });
                    }
                }

                return Task.FromResult(new PlanOperatorDistributionCost
                {
                    Selectivity = new Random().NextDouble(),
                    Cardinality = new Random().Next(1, 1000),
                    Distribution = attributeDistributionData
                });
            });
    }

    #endregion
    
    #region Helpers 
    public PopLookupTable SeedPopLookupTable(List<AttributeSpecifier> attributes)
    {
        var popLookupTable = new PopLookupTable();
        foreach (var table in attributes.Select(a => a.Table).Distinct())
        {
            var attributeDistributionData = new Dictionary<AttributeSpecifier, PlanOperatorDistributionData>();
            var random = new Random();
            foreach (var attribute in attributes.Where(a => a.IsInTable(table)))
            {
                attributeDistributionData.Add(attribute, new PlanOperatorDistributionData
                {
                    DistributionType = DistributionType.Constant,
                    Max = random.Next(50, 100),
                    Min = random.Next(0, 10),
                    Mean = random.Next(10, 20),
                    MeanBinHeight = random.NextDouble() * 10 + 10,
                    Peaks = []
                });
            }
            
            popLookupTable.Add(table, new PushdownSqlPlanOperator(Guid.NewGuid(), $"SELECT * FROM {table.TableName}")
            {
                Self = table,
                DistributionData = attributeDistributionData
            });
        }

        return popLookupTable;
    }
    
    #endregion
}
