using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class PushdownRestCallPlanOperator : PushdownPlanOperator
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

    public override bool Equals(object? other)
    {
        if (other is not PushdownRestCallPlanOperator pushdown) return false;

        return pushdown.DataSource.Equals(DataSource) && pushdown.HttpMethod.Equals(HttpMethod) && pushdown.Url.Equals(Url) &&
               (pushdown.Body?.Equals(Body) ?? true);
    }
    
    public override bool IsEquivalentTo(PlanOperator? other)
    {
        if (!base.IsEquivalentTo(other)) return false;
        return other is PushdownRestCallPlanOperator otherPushdown && Equals(otherPushdown);
    }
}