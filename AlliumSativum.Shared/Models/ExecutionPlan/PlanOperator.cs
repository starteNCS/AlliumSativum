using System.Text;

namespace AlliumSativum.Shared.Models.ExecutionPlan;

public abstract class PlanOperator
{
    public List<PlanOperator> Children { get; init; } = [];
    public double Cost { get; set; }
    public long ExpectedCardinality { get; set; }
    
    public string ToPrettyString()
    {
        var sb = new StringBuilder();
        BuildString(sb, "", true);
        return sb.ToString();
    }

    protected void BuildString(StringBuilder sb, string prefix, bool isLast)
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
    protected string GetBaseNodeInto() => $"({Cost}ms, Card: {ExpectedCardinality})";
}