using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class PushdownRestCallPlanOperator : PlanOperator
{
    public PushdownRestCallPlanOperator(Guid dataSource, string httpMethod, string url, object? body)
    {
        DataSource = dataSource;
        HttpMethod = httpMethod;
        Url = url;
        Body = body;
    }

    public required TableSpecifier Self { get; init; }
    public Guid DataSource { get; }
    public string HttpMethod { get; }
    public string Url { get; }
    public object? Body { get; }

    protected override string GetNodeInfo()
    {
        return $"PUSH-DOWN REST [{DataSource}]: '{HttpMethod} {Url}'";
    }

    protected override string GetNodeInfoHtml()
    {
        return
            $"{HtmlClasses.Bold(HtmlClasses.Colored("PUSH-DOWN REST"))} [{HtmlClasses.Italic(HtmlClasses.Colored(DataSource.ToString(), "gray"))}]: '{HttpMethod} {Url}'";
    }

    protected override double GetActualSelectivityInfo()
    {
        return 1;
    }

    public override string ToJoinPlanString()
    {
        return Self.ToString();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DataSource, HttpMethod, Url, Body);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not PushdownRestCallPlanOperator other) return false;

        return other.DataSource.Equals(DataSource) && other.HttpMethod.Equals(HttpMethod) && other.Url.Equals(Url) &&
               (other.Body?.Equals(Body) ?? true);
    }
}