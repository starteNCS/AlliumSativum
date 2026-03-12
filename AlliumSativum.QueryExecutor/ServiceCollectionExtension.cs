using AlliumSativum.QueryExecutor.PopExecutors;
using AlliumSativum.QueryExecutor.PopExecutors.Join;
using Microsoft.Extensions.DependencyInjection;

namespace AlliumSativum.QueryExecutor;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddQueryExecutor(this IServiceCollection services)
    {
        services.AddScoped<QueryExecutor>();

        services
            .AddScoped<ProjectPlanOperatorExecutor>()
            .AddScoped<FilterPlanOperatorExecutor>()
            .AddScoped<PushdownSqlPlanOperatorExecutor>()
            .AddScoped<PushdownRestPlanOperatorExecutor>()
            .AddScoped<NestedLoopJoinPlanOperatorExecutor>()
            .AddScoped<HashJoinPlanOperatorExecutor>();
        
        
        return services;
    }
}
