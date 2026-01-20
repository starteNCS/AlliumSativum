using AlliumSativum.Connectors.PostgreSQL.Statistics;
using AlliumSativum.Worker.Services;
using Dapper.Extensions.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

builder.Services.AddDapperForPostgreSQL();
builder.Services
    .AddScoped<PostgreSqlStatistics>();

var app = builder.Build();

app.MapGrpcService<MetricsService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();