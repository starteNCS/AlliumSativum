using AlliumSativum.Connectors.PostgreSQL.Planners;
using AlliumSativum.Connectors.PostgreSQL.Statistics;
using Microsoft.Extensions.DependencyInjection;

namespace AlliumSativum.Connectors.PostgreSQL.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddPostgreSqlConnector(this IServiceCollection services)
    {
        services
            .AddScoped<PostgreSqlStatistics>()
            .AddScoped<PostgreSqlPlanner>();
        
        return services;
    }
}
