using AlliumSativum.Connectors.Shared.CatalogUtils;
using Microsoft.Extensions.DependencyInjection;

namespace AlliumSativum.Connectors.Shared;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddConnectorSharedServices(this IServiceCollection services)
    {
        services.AddScoped<CatalogDistributionUtils>();
        
        return services;
    }
}
