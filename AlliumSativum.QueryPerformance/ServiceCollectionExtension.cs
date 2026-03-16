using AlliumSativum.QueryExecutor.Performance.Histogram;
using AlliumSativum.QueryServer.Utils;
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
