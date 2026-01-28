using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;

namespace AlliumSativum.Worker.Sdk;

public static class ServiceCollectionExtension
{
    // todo: somehow add multiple workers
    public static IServiceCollection AddAlliumSativumWorkerGrpcSdk(this IServiceCollection services, string workerUrl)
    {
        var channel = GrpcChannel.ForAddress(workerUrl);
        services.AddSingleton(new Metrics.MetricsClient(channel));
        services.AddSingleton(new Planner.PlannerClient(channel));
        
        services.AddScoped<MetricsApi>();
        services.AddScoped<PlannerApi>();
        
        return services;
    }
}
