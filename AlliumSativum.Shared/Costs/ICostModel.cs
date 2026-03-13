using AlliumSativum.Shared.Costs.Models;
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
    double CalculateCost(PlanOperator op);

    /// <summary>
    /// Iterates through the POP-tree and calculates the total cost of the plan, by summing up the cost of each operator
    /// </summary>
    /// <param name="planOperator"></param>
    /// <param name="fromActualCost"></param>
    /// <returns></returns>
    double TotalCost(PlanOperator? planOperator, bool fromActualCost = false);

    Task<PlanOperatorDistributionCost> GetDistributionOfExpressionAsync(BinaryOperatorExpressionNode node, Dictionary<AttributeSpecifier, PlanOperatorDistributionData> distributionData, List<PlanOperator> children);
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