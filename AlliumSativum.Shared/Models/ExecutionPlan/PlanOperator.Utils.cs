namespace AlliumSativum.Shared.Models.ExecutionPlan;

public abstract partial class PlanOperator
{
    public T? FindFirst<T>() where T : PlanOperator
    {
        if (this is T t)
        {
            return t;
        }

        foreach (var child in Children)
        {
            var result = child.FindFirst<T>();
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
