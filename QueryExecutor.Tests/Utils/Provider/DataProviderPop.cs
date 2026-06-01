using AlliumSativum.Shared.Models.ExecutionPlan;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;

namespace QueryExecutor.Tests.Utils.Provider;

public abstract class DataProviderPop : PlanOperator
{
    public DataProviderPop()
    {
    }

    public DataProviderPop(List<Dictionary<string, object>> data)
    {
        ExecutionData = new PlanOperatorExecutionData
        {
            Data = data
        };
    }

    protected override string GetNodeInfo()
    {
        throw new NotImplementedException();
    }

    protected override string GetNodeInfoHtml()
    {
        throw new NotImplementedException();
    }

    protected override double GetActualSelectivityInfo()
    {
        throw new NotImplementedException();
    }

    public override string ToJoinPlanString()
    {
        throw new NotImplementedException();
    }

    public override bool Equals(object? other)
    {
        throw new NotImplementedException();
    }
}