using AlliumSativum.QueryExecutor.PopExecutors;
using Microsoft.Extensions.DependencyInjection;

namespace AlliumSativum.QueryExecutor;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddQueryExecutor(this IServiceCollection services)
    {
        services.AddScoped<QueryExecutor>();

        services
            .AddScoped<ProjectPlanOperatorExecutor>()
            .AddScoped<PushdownSqlPlanOperatorExecutor>();
        
        
        return services;
    }
}
