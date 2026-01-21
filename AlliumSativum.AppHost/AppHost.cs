var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("catalog")
    .WithRedisInsight();

builder.AddProject<Projects.AlliumSativum_QueryServer>("Query-Service");
builder.AddProject<Projects.AlliumSativum_Worker>("Worker")
    .WithReference(redis)
    .WaitFor(redis);

builder.Build().Run();
