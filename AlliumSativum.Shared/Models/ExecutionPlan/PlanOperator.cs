namespace AlliumSativum.Shared.Models.ExecutionPlan;

public abstract class PlanOperator
{
    
}

public class PushdownSqlPlanOperator : PlanOperator
{
    public string SqlStatement {get; set; }
}
