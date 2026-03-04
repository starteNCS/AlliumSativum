using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AlliumSativum.Compiler;
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

app.MapGet("histogram/{datasource}/{relationName}/{attributeName}", async (CatalogDatabase catalog, QueryCompiler compiler, QueryExecutor queryExecutor, [FromRoute] string datasource, [FromRoute] string relationName, [FromRoute] string attributeName) =>
{
    var query = $"SELECT x.{attributeName} FROM {datasource}->{relationName} x";
    var plan = await compiler.CompileAsync(query);
    var result = await queryExecutor.ExecuteAsync(plan.RootOperator);
    var parsed = result
        .Select(x => (JsonElement) x[$"{datasource}->{relationName}.{attributeName}"])
        .Where(x => x.ValueKind == JsonValueKind.Number)
        .Select(x => x.GetDouble())
        .ToList();
    
    var attribute = await catalog.QueryAsync<AttributeEntity>($"SELECT * FROM catalog.attributes a INNER JOIN catalog.relations r ON r.id = a.relationid  WHERE a.name = '{attributeName}' LIMIT 1");
    
    var map = parsed
        .GroupBy(x => x)
        .OrderBy(x => x.Key)
        .ToDictionary(g => g.Key, g => g.Count());
    
    var plt = new ScottPlot.Plot();
    var hist = ScottPlot.Statistics.Histogram.WithBinCount(map.Count, parsed);
    var histPlot = plt.Add.Histogram(hist);
    histPlot.BarWidthFraction = 0.8;
    
    plt.Axes.Margins(bottom: 0);
    plt.Axes.Bottom.Min = map.Keys.Min();
    plt.Axes.Bottom.Max = map.Keys.Max();
    
    var svg = plt.GetSvgXml(600, 400);
    var stringBuilder = new StringBuilder();
    stringBuilder.Append("<html><body>")
        .Append(svg)
        .Append("<ul>")
        .Append($"<p>Distribution Type: {attribute.Single().DistributionType.ToString()} </p>")
        .Append($"<p>Skewness: {attribute.Single().Skewness}</p>")
        .Append($"<p>Kurtosis: {attribute.Single().Kurtosis} </p>");

    foreach (var m in map)
    {
        stringBuilder.Append($"<li>{m.Key}: {m.Value}</li>");
    }
    
    stringBuilder
        .Append("</ul>")
        .Append("</body></html>");
    
    return Results.Content(stringBuilder.ToString(), "text/html");
});

app.Run();

return;

class CompileInput
{
    public string Query { get; set; }
}