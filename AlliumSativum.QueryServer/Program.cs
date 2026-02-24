using System.Diagnostics;
using AlliumSativum.Compiler;
using AlliumSativum.Optimize;
using AlliumSativum.Parser;
using AlliumSativum.QueryExecutor;
using AlliumSativum.Semantic;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Migrations;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Token;
using AlliumSativum.Worker.Sdk;
using Azure;
using Microsoft.AspNetCore.Mvc;

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

builder.Services.AddCostModel(builder.Configuration);

builder.Services.AddQueryExecutor();

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
    
    var pretty = executionPlan.ToPrettyString(html: true);
    return Results.Content(pretty, "text/html");
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

app.MapPost("compare-selectivity", async (QueryCompiler compiler, QueryExecutor queryExecutor, [FromBody] CompileInput query) =>
{
    var executionPlan = await compiler.CompileAsync(query.Query);
    
    var parallelPlan = QueryExecutor.ToParallelStacks(executionPlan.RootOperator);

    var sw = Stopwatch.StartNew();
    var result = await queryExecutor.ExecuteAsync(parallelPlan);
    sw.Stop();

    return new
    {
        Cardinality = new
        {
            Expected = executionPlan.RootOperator.ExpectedCardinality,
            Actual = result.Count,
            Precision = (double) result.Count / executionPlan.RootOperator.ExpectedCardinality
        },
        Cost = new
        {
            Expected = executionPlan.RootOperator.Cost,
            Actual = sw.ElapsedMilliseconds,
            Precision = sw.ElapsedMilliseconds / executionPlan.RootOperator.Cost
        }
    };
});

app.Run();

return;

class CompileInput
{
    public string Query { get; set; }
}