using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Enums;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Worker.Sdk.Extensions;
using AlliumSativum.Worker.Strategies;
using Grpc.Core;

namespace AlliumSativum.Worker.Services;

public sealed class ExecutorService : Executor.ExecutorBase
{
    private readonly CatalogDatabase _catalog;
    private readonly ExecutorStrategy _strategy;

    public ExecutorService(
        ExecutorStrategy strategy,
        CatalogDatabase catalog)
    {
        _strategy = strategy;
        _catalog = catalog;
    }

    public override async Task<GExecutorWrapper> Execute(GPlanOperator request, ServerCallContext context)
    {
        var dataSource = await _catalog.GetDataSourceAsync(GetDataSourceIdFromPlanOperator(request));
        if (dataSource is null)
            throw new AsSQLExecuteException("Data source not found for the given plan operator.",
                ConnectorType.Postgres);

        var planOperator = request.FromGrpcModel();
        if (planOperator is null)
            throw new AsSQLExecuteException("Failed to convert the gRPC plan operator to the internal model.",
                ConnectorType.Postgres);

        var executor = _strategy.GetPlannerOfConnector(dataSource.Connector);
        var result = await executor.ExecuteAsync(planOperator);
        return result.ToGrpcModel();
    }

    private static Guid GetDataSourceIdFromPlanOperator(GPlanOperator pop)
    {
        var idString = pop.OperatorTypeCase switch
        {
            GPlanOperator.OperatorTypeOneofCase.PushdownSql => pop.PushdownSql.DatasourceId,
            GPlanOperator.OperatorTypeOneofCase.PushdownRestCall => pop.PushdownRestCall.DatasourceId,
            _ => throw new ArgumentException("Invalid plan operator type. Did you forget to add it to the switch?",
                nameof(pop))
        };

        return Guid.Parse(idString);
    }
}