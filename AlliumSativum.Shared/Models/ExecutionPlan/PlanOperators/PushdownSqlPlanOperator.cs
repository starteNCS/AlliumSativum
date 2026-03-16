using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class PushdownSqlPlanOperator : PlanOperator
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

    public override int GetHashCode()
    {
        return HashCode.Combine(DataSource, SqlStatement);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not PushdownSqlPlanOperator other) return false;

        return other.DataSource.Equals(DataSource) && other.SqlStatement.Equals(SqlStatement);
    }
}