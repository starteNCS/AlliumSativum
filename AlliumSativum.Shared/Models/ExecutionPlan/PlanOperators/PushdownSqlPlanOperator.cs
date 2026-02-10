namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;

public class PushdownSqlPlanOperator : PlanOperator
{
    public Guid DataSource { get;  }
    public string SqlStatement { get; }

    public PushdownSqlPlanOperator(Guid dataSource, string sqlStatement)
    {
        DataSource = dataSource;
        SqlStatement = sqlStatement;
    }

    protected override string GetNodeInfo() => $"{GetBaseNodeInto()} PUSH-DOWN SQL [{DataSource}]: '{SqlStatement}'";

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