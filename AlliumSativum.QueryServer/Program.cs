using AlliumSativum.Compiler;
using AlliumSativum.Optimize;
using AlliumSativum.Parser;
using AlliumSativum.Semantic;
using AlliumSativum.Token;

var builder = WebApplication.CreateBuilder(args);

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
    var parsedQuery = compiler.Compile("SELECT c.name FROM erp->customers c WHERE c.orders_count > 10");
    return parsedQuery;
});

app.Run();