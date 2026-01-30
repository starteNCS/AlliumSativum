using AlliumSativum.Shared.Models.IntermediateModels.Expressions;

namespace AlliumSativum.Shared.Models.ExecutionPlan;

public abstract class PlanOperator
{
    public List<PlanOperator> Children { get; init; } = [];
    public double Cost { get; set; }
    
    protected string ToChildrenString() => string.Join(", ", Children.Select(x => x.ToString()));
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

    public override string ToString() => $"pushdown({DataSource}, '{SqlStatement}')";
}

public class WherePlanOperator : PlanOperator
{
    public IExpressionNode Expression { get; }

    public WherePlanOperator(IExpressionNode expression)
    {
        Expression = expression;
    }
    
    public override string ToString() => $"where({Expression}, {ToChildrenString()})";
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
    
    public override string ToString() => $"join({Left}, {Right})";
}