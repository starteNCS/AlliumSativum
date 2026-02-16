using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
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
}

public static class CostModelExtensions
{
    public static IServiceCollection AddCostModel(this IServiceCollection services)
    {
        services.AddScoped<ICostModel, DefaultCostModel>();

        return services;
    }
}