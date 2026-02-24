using System.Text;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;

namespace AlliumSativum.Shared.Models.ExecutionPlan;

public abstract class PlanOperator
{
    public List<PlanOperator> Children { get; init; } = [];
    public double Cost { get; set; }
    public long ExpectedCardinality { get; set; }
    public double Selectivity { get; set; } = 1;

    public PlanOperatorExecutionData ExecutionData { get; set; } = new();
    
    public string ToPrettyString(bool html = false)
    {
        var sb = new StringBuilder();
        if (html)
        {
            sb.Append("<div style=\"font-family: monospace; white-space: pre;\">");
            BuildStringHtml(sb, "", true);
            sb.Append("</div>");
        }
        else
        {
            BuildString(sb, "", true);
        }
        
        return sb.ToString();
    }

    protected void BuildString(StringBuilder sb, string prefix, bool isLast)
    {
        sb.Append(prefix);
        sb.Append(isLast ? "└── " : "├── ");
        sb.AppendLine(GetNodeInfo());

        // Add second line for POP's string aligned under the branch
        sb.Append(prefix);
        sb.Append(isLast ? "    " : "│   ");
        sb.AppendLine(GetBaseNodeInfo());

        // 2. Prepare prefix for children
        var childPrefix = prefix + (isLast ? "    " : "│   ");

        // 3. Recurse
        for (var i = 0; i < Children.Count; i++)
        {
            var childIsLast = (i == Children.Count - 1);
            Children[i].BuildString(sb, childPrefix, childIsLast);
        }
    }
    
    protected void BuildStringHtml(StringBuilder sb, string prefix, bool isLast)
    {
        sb.Append(prefix);
        sb.Append(isLast ? "└── " : "├── ");
        sb.AppendLine(GetNodeInfoHtml());

        // Add second line for POP's string aligned under the branch
        sb.Append(prefix);
        sb.Append(isLast ? "    " : "│   ");
        sb.AppendLine(GetHtmlBaseNodeInfo());

        // 2. Prepare prefix for children
        var childPrefix = prefix + (isLast ? "    " : "│   ");

        // 3. Recurse
        for (var i = 0; i < Children.Count; i++)
        {
            var childIsLast = (i == Children.Count - 1);
            Children[i].BuildStringHtml(sb, childPrefix, childIsLast);
        }
    }

    protected abstract string GetNodeInfo();
    protected abstract string GetNodeInfoHtml();
    private string GetBaseNodeInfo() => $"ED: {Cost:F2}ms, C: {ExpectedCardinality}, S: {Selectivity}";
    private string GetHtmlBaseNodeInfo() => $"ED: {HtmlClasses.Colored(Cost.ToString("F3"), color: "coral")}ms, C: {HtmlClasses.Colored(ExpectedCardinality.ToString(), color: "yellowgreen")}, S: {HtmlClasses.Colored(Selectivity.ToString("F5"), color: "olive")}";
}