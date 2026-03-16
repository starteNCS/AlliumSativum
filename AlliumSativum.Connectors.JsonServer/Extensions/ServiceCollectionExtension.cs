using AlliumSativum.Connectors.JsonServer.Executor;
using AlliumSativum.Connectors.JsonServer.Planner;
using AlliumSativum.Connectors.JsonServer.Statistics;
using Microsoft.Extensions.DependencyInjection;

namespace AlliumSativum.Connectors.JsonServer.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddJsonServerConnector(this IServiceCollection services)
    {
        services
            .AddScoped<JsonServerStatistics>()
            .AddScoped<JsonServerPlanner>()
            .AddScoped<JsonServerExecutor>();

        return services;
    }
}