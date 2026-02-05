using AlliumSativum.Connectors.TicketSystem.Planner;
using AlliumSativum.Connectors.TicketSystem.Statistics;
using Microsoft.Extensions.DependencyInjection;

namespace AlliumSativum.Connectors.TicketSystem.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddTicketSystemConnector(this IServiceCollection services)
    {
        services
            .AddScoped<TicketSystemStatistics>()
            .AddScoped<TicketSystemPlanner>();
        
        return services;
    }
}
