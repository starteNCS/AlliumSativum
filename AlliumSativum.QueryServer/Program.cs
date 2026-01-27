using AlliumSativum.Compiler;
using AlliumSativum.Optimize;
using AlliumSativum.Parser;
using AlliumSativum.Semantic;
using AlliumSativum.Token;
using AlliumSativum.Worker.Sdk;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddAlliumSativumWorkerGrpcSdk(builder.Configuration["WorkerUrl"] ?? throw new ArgumentException("Worker Url is required!"));

builder.Services
    .AddScoped<QueryCompiler>()
    .AddScoped<TokenQueryParser>()
    .AddScoped<SemanticTransformer>()
    .AddScoped<Tokenizer>()
    .AddScoped<Optimizer>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/compile", (QueryCompiler compiler) =>
{
    var parsedQuery = compiler.Compile("SELECT c.name, o.gross FROM erp->customers c WHERE c.orders_count > 10 INNER JOIN erp->orders o ON o.customer_id = c.id");
    return parsedQuery;
});
app.MapGet("/metrics", async (MetricsApi metrics) =>
{
    await metrics.TriggerMetricsScrapeAsync(Guid.Parse("6e69646b-47be-4e80-aa02-06b48b8c7253"));
});

app.Run();