namespace AlliumSativum.Shared.Models.ExecutionPlan;

public abstract partial class PlanOperator
{
    public virtual bool IsEquivalentTo(PlanOperator? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        if (this.GetType() != other.GetType()) return false;

        if (Children.Count != other.Children.Count) return false;
        if (!Equals(other)) return false;

        for (int i = 0; i < Children.Count; i++)
        {
            if (!Children[i].IsEquivalentTo(other.Children[i]))
            {
                return false;
            }
        }

        return true;
    }
}