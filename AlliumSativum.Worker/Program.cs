using System.Reflection;
using AlliumSativum.Connectors.PostgreSQL.DatabaseConnectors;
using AlliumSativum.Connectors.PostgreSQL.Statistics;
using AlliumSativum.Shared.Migrations;
using AlliumSativum.Worker.Services;
using Dapper.Extensions.PostgreSql;
using DbUp;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddGrpc();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("catalog-cache");
    options.InstanceName = "Worker_";
});

builder.AddCatalogDatabase(builder.Configuration.GetConnectionString("catalog-database") ??
                           throw new ArgumentException("Catalog connection must be provided"));

builder.Services
    .AddScoped<DatasourceDatabase>()
    .AddScoped<PostgreSqlStatistics>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGrpcService<MetricsService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

return;

