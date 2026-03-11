using AlliumSativum.Shared.Costs.Settings;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlliumSativum.Shared.Costs;

public interface ICostModel
{
    /// <summary>
    /// Uses selinger style selectivity estimation, which is very basic, but should be good enough for most cases
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    Task<double> GetSelectivityAsync(BinaryOperatorExpressionNode node);

    /// <summary>
    /// Caclualtes the expected cardinality after applying a given filter
    /// </summary>
    /// <param name="node"></param>
    /// <param name="previousCardinality"></param>
    /// <returns></returns>
    Task<(long Cardinality, double Selectivity)> CalculateExpectedCardinalityAsync(BinaryOperatorExpressionNode node, long previousCardinality);

    /// <summary>
    /// Caclualtes the expected cardinality after applying a given filter
    /// </summary>
    /// <param name="join"></param>
    /// <param name="previousCardinality"></param>
    /// <returns></returns>
    Task<(long Cardinality, double Selectivity)> CalculateExpectedCardinalityAsync(JoinPlanOperator join);

    double CalculateCost(PlanOperator op);

    /// <summary>
    /// Iterates through the POP-tree and calculates the total cost of the plan, by summing up the cost of each operator
    /// </summary>
    /// <param name="planOperator"></param>
    /// <returns></returns>
    double TotalCost(PlanOperator planOperator);

    (Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distribution, double selectivity) GetDistributionOfExpression(BinaryOperatorExpressionNode node, Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData);
}

public static class CostModelExtensions
{
    public static IServiceCollection AddCostModel(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICostModel, DefaultCostModel>();
        services.AddOptions<CostModelSettings>()
            .Bind(configuration.GetSection("CostModel"));

        return services;
    }
}