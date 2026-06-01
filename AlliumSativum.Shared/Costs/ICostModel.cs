using AlliumSativum.Shared.Costs.Models;
using AlliumSativum.Shared.Costs.Settings;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlliumSativum.Shared.Costs;

public interface ICostModel
{
    double CalculateCost(PlanOperator op);

    /// <summary>
    ///     Iterates through the POP-tree and calculates the total cost of the plan, by summing up the cost of each operator
    /// </summary>
    /// <param name="planOperator"></param>
    /// <param name="fromActualCost"></param>
    /// <returns></returns>
    double TotalCost(PlanOperator? planOperator, bool fromActualCost = false);

    /// <summary>
    ///     Calculates the new distribution of the attributes after applying the given expression node as a filter on the
    ///     children operators.
    ///     May recursively call itself for sub-expressions in case of AND/OR expressions.
    /// </summary>
    /// <param name="node">The node to calculate distribution for</param>
    /// <param name="distributionData">The children distribution data</param>
    /// <param name="children">The childrens</param>
    /// <returns>The newly calculated disitrbution data</returns>
    /// <exception cref="NotImplementedException"></exception>
    /// <exception cref="ArgumentException">An unsupported expression was used</exception>
    Task<PlanOperatorDistributionCost> GetDistributionOfExpressionAsync(BinaryOperatorExpressionNode node,
        Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData, List<PlanOperator> children);

    /// <summary>
    ///     Reconstructs a distribution from the given distribution data
    /// </summary>
    /// <param name="distributionData">The attribtues distribution data</param>
    /// <returns>The histogram of the attribute</returns>
    Dictionary<double, double> ReconstructDistribution(PlanOperatorDistributionData distributionData);
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