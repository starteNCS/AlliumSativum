using System.Diagnostics;
using System.Dynamic;
using System.Text.Json;
using AlliumSativum.Connectors.Shared.Interfaces;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Enums;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.Executor;
using AlliumSativum.Shared.Utils;

namespace AlliumSativum.Connectors.TicketSystem.Executor;

public sealed class TicketSystemExecutor : IWorkerExecutor
{
    private readonly CatalogDatabase _catalog;
    private readonly IHttpClientFactory _httpClientFactory;

    public TicketSystemExecutor(
        CatalogDatabase catalog,
        IHttpClientFactory httpClientFactory)
    {
        _catalog = catalog;
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<ExecutorWrapper> ExecuteAsync(PlanOperator @operator)
    {
        if (@operator is not PushdownRestCallPlanOperator pushdown)
        {
            throw new AsSQLExecuteException("Invalid plan operator type for TicketSystemExecutor. Expected PushdownRestCallPlanOperator.", ConnectorType.TicketSystem);
        }
        
        var dataSource = await _catalog.GetDataSourceAsync(@pushdown.DataSource);
        if (dataSource is null)
        {
            throw new AsSQLExecuteException($"Data source with id {@pushdown.DataSource} not found", ConnectorType.TicketSystem);
        }
        
        var httpClient = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(@pushdown.HttpMethod), @pushdown.Url);
        if(@pushdown.Body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(@pushdown.Body));
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResult = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(responseContent);
        stopwatch.Stop();
        
        return new ExecutorWrapper
        {
            PlanOperator = @operator,
            Result = jsonResult ?? [],
            FactualCardinality = jsonResult?.Count ?? 0,
            FactualCost = stopwatch.ElapsedMilliseconds,
        };
    }
}
