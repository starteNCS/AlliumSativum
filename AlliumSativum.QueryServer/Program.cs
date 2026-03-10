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

app.MapPost("histogram/{datasource}/{relationName}/{attributeName}", async (CatalogDatabase catalog, QueryCompiler compiler, QueryExecutor queryExecutor,
    [FromBody] List<CompileInput> queries) =>
{
    List<Color> colors =
    [
        Color.FromHex("#6CD4FF"),
        Color.FromHex("#FE938C"),
    ];
    var plt = new ScottPlot.Plot();

    List<AttributeEntity> attributes = [];
    List<Dictionary<double, int>> maps = [];
    double min = 0, max = 0;
    
    int index = 0;
    foreach (var query in queries)
    {
        var plan = await compiler.CompileAsync(query.Query);
        if (plan.RootOperator is not ProjectPlanOperator pop)
        {
            return Results.Content("<html><body><p>Only simple select queries are supported</p></body></html>", "text/html");
        }
        if (pop.Attributes.Count != 1)
        {
            return Results.Content("<html><body><p>You need to project to one operator here</p></body></html>", "text/html");
        }
    
        var result = await queryExecutor.ExecuteAsync(plan.RootOperator);
        var parsed = result
            .Select(x => (JsonElement?) x.Single().Value)
            .Where(x => x is null || x.Value.ValueKind == JsonValueKind.Number)
            .Select(x =>
            {
                if (x is null || !x.Value.TryGetDouble(out var value))
                {
                    return double.NaN;
                }

                return value;
            })
            .ToList();

        var (attribute, modes) = DistributionUtils.CalculateDistribution(parsed.Select(x => (double?)x).ToList(), new AttributeEntity());
        attributes.Add(attribute);
        
        var map = parsed
            .GroupBy(x => x)
            .OrderBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.Count());
        min = map.Keys.Min() < min ? map.Keys.Min() : min;
        max = map.Keys.Max() > max ? map.Keys.Max() : max;
        maps.Add(map);
    
        var hist = ScottPlot.Statistics.Histogram.WithBinCount(map.Count, parsed);
        var histPlot = plt.Add.Histogram(hist, colors[index]);
        histPlot.BarWidthFraction = 0.8;

        index++;
    }
    
    plt.Axes.Margins(bottom: 0);
    plt.Axes.Bottom.Min = min;
    plt.Axes.Bottom.Max = min;
    
    var svg = plt.GetSvgXml(600, 400);
    var stringBuilder = new StringBuilder();
    stringBuilder.Append("<html><body>")
        .Append(svg);
        // .Append("<ul>")
        // .Append($"<p>Distribution Type: {attribute.DistributionType.ToString()} </p>")
        // .Append($"<p>Mean: {attribute.Mean} </p>")
        // .Append($"<p>Standard Deviation: {attribute.StandardDeviation} </p>")
        // .Append($"<p>Coefficient of Variance: {attribute.StandardDeviation / attribute.Mean} </p>")
        // .Append($"<p>Skewness: {attribute.Skewness}</p>")
        // .Append($"<p>Kurtosis: {attribute.Kurtosis} </p>");



        stringBuilder.Append("<table><tr><th>Key</th>");
        for (int i = 0; i < maps.Count; i++)
        {
            stringBuilder.Append("<th>Query " + (i + 1) + "</th>");
        }
        stringBuilder.Append("</tr>");
        
    for (double i = min; i < max; i++)
    {
        stringBuilder.Append($"<tr><td>{i}</td> ");
        foreach (var map in maps)
        {
            var entry = map.Where(kv => kv.Key >= i).OrderBy(kv => kv.Key).FirstOrDefault();
            stringBuilder.Append($"<td>{entry.Value}</td>");
        }
        stringBuilder.Append("</tr>");
    }
    
    stringBuilder
        // .Append("</ul>")
        .Append("</body></html>");
    
    return Results.Content(stringBuilder.ToString(), "text/html");
});

app.Run();

return;

class CompileInput
{
    public string Query { get; set; }
}