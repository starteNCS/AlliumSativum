using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AlliumSativum.Compiler;
using AlliumSativum.Connectors.Shared;
using AlliumSativum.Optimize;
using AlliumSativum.Parser;
using AlliumSativum.QueryExecutor;
using AlliumSativum.QueryExecutor.Performance;
using AlliumSativum.Semantic;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Migrations;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Utils;
using AlliumSativum.Token;
using AlliumSativum.Worker.Sdk;
using Azure;
using Microsoft.AspNetCore.Mvc;
using ScottPlot;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddCatalogDatabase(builder.Configuration.GetConnectionString("catalog-database") ??
                           throw new ArgumentException("Catalog connection must be provided"));
builder.Services.AddAlliumSativumWorkerGrpcSdk(builder.Configuration["WorkerUrl"] ?? throw new ArgumentException("Worker Url is required!"));

builder.Services
    .AddScoped<QueryCompiler>()
    .AddScoped<TokenQueryParser>()
    .AddScoped<SemanticTransformer>()
    .AddScoped<Tokenizer>()
    .AddOptimizer();

builder.Services
    .AddScoped<JoinSelectivityPerformanceChecker>();

builder.Services.AddCostModel(builder.Configuration);

builder.Services.AddQueryExecutor();

builder.Services.AddControllers();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapPost("/compile", async (QueryCompiler compiler, [FromBody] CompileInput query) =>
{
    var executionPlan = await compiler.CompileAsync(query.Query);
    
    var pretty = executionPlan.ToPrettyString(html: true);
    return Results.Content(pretty, "text/html");
});

app.MapGet("/metrics/all", async (MetricsApi metrics, CatalogDatabase catalog) =>
{
    var sources = await catalog.QueryAsync<DataSourceEntity>("SELECT * FROM catalog.datasources");

    foreach (var source in sources)
    {
        await metrics.TriggerMetricsScrapeAsync(source.Id);
    }
});

app.MapGet("/metrics/{datasourceId:guid}", async (MetricsApi metrics, [FromRoute] Guid datasourceId) =>
{
    await metrics.TriggerMetricsScrapeAsync(datasourceId);
});

app.MapPost("execute", async (QueryCompiler compiler, QueryExecutor queryExecutor, [FromBody] CompileInput query) =>
{
    var executionPlan = await compiler.CompileAsync(query.Query);
    
    var parallelPlan = QueryExecutor.ToParallelStacks(executionPlan.RootOperator);
    var result = await queryExecutor.ExecuteAsync(parallelPlan);

    return result;
});

app.MapPost("execute-return-plan", async (QueryCompiler compiler, QueryExecutor queryExecutor, [FromBody] CompileInput query) =>
{
    var executionPlan = await compiler.CompileAsync(query.Query);
    
    var parallelPlan = QueryExecutor.ToParallelStacks(executionPlan.RootOperator);
    var result = await queryExecutor.ExecuteAsync(parallelPlan);

    return executionPlan.RootOperator.ToPrettyString(html: true, includeActual: true);
});

app.MapGet("selectivity-performance",
    async (JoinSelectivityPerformanceChecker performanceChecker) => await performanceChecker.ExecuteJoinSelectivityPerformanceCheckerAsync());

app.Run();

return;

public class CompileInput
{
    public string Query { get; set; }
}