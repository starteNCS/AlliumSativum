using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class PushdownRestCallPlanOperator : PlanOperator
{
    public required TableSpecifier Self { get; init; }
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

    protected override string GetNodeInfo() => $"PUSH-DOWN REST [{DataSource}]: '{HttpMethod} {Url}'";
    protected override string GetNodeInfoHtml() => $"{HtmlClasses.Bold(HtmlClasses.Colored("PUSH-DOWN REST"))} [{HtmlClasses.Italic(HtmlClasses.Colored(DataSource.ToString(), color: "gray"))}]: '{HttpMethod} {Url}'";
    
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