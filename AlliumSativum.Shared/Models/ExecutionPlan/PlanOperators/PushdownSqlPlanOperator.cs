using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class PushdownSqlPlanOperator : PlanOperator
{
    public required TableSpecifier Self { get; init; }
    public Guid DataSource { get;  }
    public string SqlStatement { get; }

    public PushdownSqlPlanOperator(Guid dataSource, string sqlStatement)
    {
        DataSource = dataSource;
        SqlStatement = sqlStatement;
    }

    protected override string GetNodeInfo() => $"PUSH-DOWN SQL [{DataSource}]: '{SqlStatement}'";
    protected override string GetNodeInfoHtml() => $"{HtmlClasses.Bold(HtmlClasses.Colored("PUSH-DOWN SQL"))} [{HtmlClasses.Italic(HtmlClasses.Colored(DataSource.ToString(), color: "gray"))}]: '{SqlStatement}'";
    protected override double GetActualSelectivityInfo() => 1;

    public override int GetHashCode()
    {
        return HashCode.Combine(DataSource, SqlStatement);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not PushdownSqlPlanOperator other)
        {
            return false;
        }
        
        return other.DataSource.Equals(DataSource) && other.SqlStatement.Equals(SqlStatement);
    }
}