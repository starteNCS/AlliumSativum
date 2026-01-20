using AlliumSativum.Compiler;
using AlliumSativum.Optimize;
using AlliumSativum.Parser;
using AlliumSativum.Semantic;
using AlliumSativum.Token;
using AlliumSativum.Worker.Sdk;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAlliumSativumWorkerGrpcSdk(builder.Configuration["WorkerUrl"] ?? throw new ArgumentException("Worker Url is required!"));

builder.Services
    .AddScoped<QueryCompiler>()
    .AddScoped<TokenQueryParser>()
    .AddScoped<SemanticTransformer>()
    .AddScoped<Tokenizer>()
    .AddScoped<Optimizer>();

var app = builder.Build();

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
    await metrics.TriggerMetricsScrapeAsync(1);
});

app.Run();