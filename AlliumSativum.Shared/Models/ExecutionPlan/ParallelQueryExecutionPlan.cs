namespace AlliumSativum.Shared.Models.ExecutionPlan;

public sealed class ParallelQueryExecutionPlan
{
    /// <summary>
    /// All parallelizable branches of the QExP
    /// </summary>
    public List<ParallelQueryExecutionPlan> AwaitableStacks { get; set; } 
    
    /// <summary>
    /// The continuation stack that will be executed after all parallel branches have completed.
    /// This is also run, when no other <seealso cref="AwaitableStacks"/> are present (case: leaf of tree)
    /// </summary>
    public Stack<PlanOperator> Continuation { get; set; }
}
