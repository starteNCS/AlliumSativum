namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class PushdownRestCallPlanOperator : PlanOperator
{
    public Guid DataSource { get;  }
    public string HttpMethod { get; }
    public string Url { get; }
    public object? Body { get; }

    public PushdownRestCallPlanOperator(Guid dataSource, string httpMethod, string url, object? body)
    {
        DataSource = dataSource;
        HttpMethod = httpMethod;
        Url = url;
        Body = body;
    }

    protected override string GetNodeInfo() => $"{GetBaseNodeInto()} PUSH-DOWN REST [{DataSource}]: '{HttpMethod} {Url}'";
    
    public override int GetHashCode()
    {
        return HashCode.Combine(DataSource, HttpMethod, Url, Body);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not PushdownRestCallPlanOperator other)
        {
            return false;
        }
        
        return other.DataSource.Equals(DataSource) && other.HttpMethod.Equals(HttpMethod) && other.Url.Equals(Url) &&
               (other.Body?.Equals(Body) ?? true);
    }
}