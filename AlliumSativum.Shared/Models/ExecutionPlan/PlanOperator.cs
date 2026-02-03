using System.Text;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan;

public abstract class PlanOperator
{
    public List<PlanOperator> Children { get; init; } = [];
    public double Cost { get; set; }
    
    public string ToPrettyString()
    {
        var sb = new StringBuilder();
        BuildString(sb, "", true);
        return sb.ToString();
    }

    protected virtual void BuildString(StringBuilder sb, string prefix, bool isLast)
    {
        sb.Append(prefix);
        sb.Append(isLast ? "└── " : "├── ");
        sb.AppendLine(GetNodeInfo());

        // 2. Prepare prefix for children
        string childPrefix = prefix + (isLast ? "    " : "│   ");

        // 3. Recurse
        for (int i = 0; i < Children.Count; i++)
        {
            bool childIsLast = (i == Children.Count - 1);
            Children[i].BuildString(sb, childPrefix, childIsLast);
        }
    }

    protected abstract string GetNodeInfo();
}

public class PushdownSqlPlanOperator : PlanOperator
{
    public Guid DataSource { get;  }
    public string SqlStatement { get; }

    public PushdownSqlPlanOperator(Guid dataSource, string sqlStatement)
    {
        DataSource = dataSource;
        SqlStatement = sqlStatement;
    }

    protected override string GetNodeInfo() => $"({Cost}) PUSH-DOWN [{DataSource}]: '{SqlStatement}'";
}

public class WherePlanOperator : PlanOperator
{
    public IExpressionNode Expression { get; }

    public WherePlanOperator(IExpressionNode expression)
    {
        Expression = expression;
    }
    
    protected override string GetNodeInfo() => $"({Cost}) FILTER: {Expression}";
}

public class JoinPlanOperator : PlanOperator
{
    public PlanOperator Left { get; }
    public PlanOperator Right { get;  }

    public JoinPlanOperator(PlanOperator left, PlanOperator right)
    {
        Left = left;
        Right = right;
        
        base.Children.AddRange(left, right);
    }
    
    // override to avoid some outer class to add more children
    public new IReadOnlyList<PlanOperator> Children => base.Children;
    
    protected override string GetNodeInfo() => $"({Cost}) INNER JOIN";
}