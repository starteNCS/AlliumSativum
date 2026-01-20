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
        
        services.AddScoped<MetricsApi>();
        
        return services;
    }
}
