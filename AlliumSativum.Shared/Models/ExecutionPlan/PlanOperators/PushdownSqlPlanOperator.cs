using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class PushdownSqlPlanOperator : PushdownPlanOperator
{
    public PushdownSqlPlanOperator(Guid dataSource, string sqlStatement)
    {
        DataSource = dataSource;
        SqlStatement = sqlStatement;
    }

    public required TableSpecifier Self { get; init; }
    public Guid DataSource { get; }
    public string SqlStatement { get; }

    protected override string GetNodeInfo()
    {
        return $"PUSH-DOWN SQL [{DataSource}]: '{SqlStatement}'";
    }

    protected override string GetNodeInfoHtml()
    {
        return
            $"{HtmlClasses.Bold(HtmlClasses.Colored("PUSH-DOWN SQL"))} [{HtmlClasses.Italic(HtmlClasses.Colored(DataSource.ToString(), "gray"))}]: '{SqlStatement}'";
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
        return HashCode.Combine(DataSource, SqlStatement);
    }

    public override bool Equals(object? other)
    {
        if (other is not PushdownSqlPlanOperator pushdown) return false;

        return pushdown.DataSource.Equals(DataSource) && pushdown.SqlStatement.Equals(SqlStatement);
    }
    
    public override bool IsEquivalentTo(PlanOperator? other)
    {
        if (!base.IsEquivalentTo(other)) return false;
        return other is PushdownSqlPlanOperator otherPushdown && Equals(otherPushdown);
    }
}