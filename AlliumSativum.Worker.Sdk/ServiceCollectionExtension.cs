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
        services.AddSingleton(new Executor.ExecutorClient(channel));
        
        services.AddScoped<MetricsApi>();
        services.AddScoped<IPlannerApi, PlannerApi>();
        services.AddScoped<IExecutorApi, ExecutorApi>();
        
        
        return services;
    }
}
