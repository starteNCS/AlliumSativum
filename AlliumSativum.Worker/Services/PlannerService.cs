using AlliumSativum.Connectors.Shared.Interfaces;
using Grpc.Core;

namespace AlliumSativum.Worker.Services;

public sealed class PlannerService : Planner.PlannerBase
{
    private readonly ILogger<PlannerService> _logger;
    private readonly IPlanner _planner;

    public PlannerService(ILogger<PlannerService> logger, IPlanner planner)
    {
        _logger = logger;
        _planner = planner;
    }
    
    public override Task<Void> Plan(GSelectBaseModel request, ServerCallContext context)
    {
        _logger.LogDebug("Begin query planning for: {FromTable}", request.From.TableName);
        return Task.FromResult(new Void());
    }
}
