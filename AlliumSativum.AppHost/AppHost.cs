using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("catalog-dbms")
    .WithHostPort(5432)
    .WithDataVolume();

var database = postgres
    .AddDatabase("catalog-database");

var redis = builder.AddRedis("catalog-cache")
    .WithRedisInsight();

var worker = builder.AddProject<AlliumSativum_Worker>("Worker")
    .WithReference(redis)
    .WithReference(database)
    .WaitFor(redis)
    .WaitFor(database);

builder.AddProject<AlliumSativum_QueryServer>("Query-Service")
    .WithReference(database)
    .WithReference(worker)
    .WaitFor(worker);

await builder.Build().RunAsync();