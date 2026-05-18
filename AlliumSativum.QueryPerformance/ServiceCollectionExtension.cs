using AlliumSativum.QueryPerformance.Histogram;
using AlliumSativum.QueryPerformance.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace AlliumSativum.QueryPerformance;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddQueryBenchmark(this IServiceCollection services)
    {
        services
            .AddScoped<DataUtils>()
            .AddScoped<ReconstructionDistanceService>();
        return services;
    }
}