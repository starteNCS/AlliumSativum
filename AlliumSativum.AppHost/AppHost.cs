var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("catalog-dbms")
    .WithHostPort(5432)
    .WithDataVolume();
    
var database = postgres
    .AddDatabase("catalog-database");

var redis = builder.AddRedis("catalog-cache")
    .WithRedisInsight();

builder.AddProject<Projects.AlliumSativum_QueryServer>("Query-Service");
builder.AddProject<Projects.AlliumSativum_Worker>("Worker")
    .WithReference(redis)
    .WithReference(database)
    .WaitFor(redis)
    .WaitFor(database);

builder.Build().Run();
