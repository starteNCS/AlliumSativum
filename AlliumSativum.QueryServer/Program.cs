using AlliumSativum.Compiler;
using AlliumSativum.Optimize;
using AlliumSativum.Parser;
using AlliumSativum.Semantic;
using AlliumSativum.Token;
using AlliumSativum.Worker.Sdk;
using Microsoft.AspNetCore.Mvc;

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

app.MapPost("/compile", async (QueryCompiler compiler, [FromBody] CompileInput query) =>
{
    var executionPlan = await compiler.CompileAsync(query.Query);
    return executionPlan.RootOperator.ToPrettyString();
});
app.MapGet("/metrics/{datasourceId:guid}", async (MetricsApi metrics, [FromRoute] Guid datasourceId) =>
{
    await metrics.TriggerMetricsScrapeAsync(datasourceId);
});

app.Run();

return;

class CompileInput
{
    public string Query { get; set; }
}