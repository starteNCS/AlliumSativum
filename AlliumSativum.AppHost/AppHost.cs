var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AlliumSativum_QueryServer>("Query-Service");
builder.AddProject<Projects.AlliumSativum_Worker>("Worker");

builder.Build().Run();
