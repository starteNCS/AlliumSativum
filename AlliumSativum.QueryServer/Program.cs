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
app.MapGet("execute", async (QueryExecutor queryExecutor) =>
{
    var result = await queryExecutor.ExecuteAsync(new PushdownSqlPlanOperator(Guid.Parse("6e69646b-47be-4e80-aa02-06b48b8c7253"),
        "SELECT employees.first_name, employees.last_name, employees.id, customers.company_name FROM employees INNER JOIN customers ON (customers.id = employees.customer_id)"));

    return result;
});

app.Run();

return;

class CompileInput
{
    public string Query { get; set; }
}