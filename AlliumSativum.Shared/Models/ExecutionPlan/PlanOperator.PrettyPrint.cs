using System.Text;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;

namespace AlliumSativum.Shared.Models.ExecutionPlan;

public abstract partial class PlanOperator
{
    public string ToPrettyString(bool html = false, bool includeActual = false)
    {
        var sb = new StringBuilder();
        if (html)
        {
            sb.Append("<div style=\"font-family: monospace; white-space: pre;\">");
            BuildStringHtml(sb, "", true, includeActual);
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
    
    protected void BuildStringHtml(StringBuilder sb, string prefix, bool isLast, bool includeActual = false)
    {
        sb.Append(prefix);
        sb.Append(isLast ? "└── " : "├── ");
        sb.AppendLine(GetNodeInfoHtml());

        // Add second line for POP's string aligned under the branch
        sb.Append(prefix);
        sb.Append(isLast ? "    " : "│   ");
        sb.AppendLine(GetHtmlBaseNodeInfo(includeActual));

        // 2. Prepare prefix for children
        var childPrefix = prefix + (isLast ? "    " : "│   ");

        // 3. Recurse
        for (var i = 0; i < Children.Count; i++)
        {
            var childIsLast = (i == Children.Count - 1);
            Children[i].BuildStringHtml(sb, childPrefix, childIsLast, includeActual);
        }
    }

    protected abstract string GetNodeInfo();
    protected abstract string GetNodeInfoHtml();
    private string GetBaseNodeInfo() => $"ED: {Cost:F2}ms, C: {ExpectedCardinality}, S: {Selectivity}";
    private string GetHtmlBaseNodeInfo(bool includeActual = false)
    {
        var sb = new StringBuilder();
        sb.Append("ED: ")
            .Append(HtmlClasses.Colored(Cost.ToString("F3"), color: "coral"))
            .Append("ms");
        if (includeActual)
        {
            sb.Append(" (actual ")
                .Append(ExecutionData.ActualCost.ToString("F3"))
                .Append("ms)");
        }
        sb.Append(", C: ")
            .Append(HtmlClasses.Colored(ExpectedCardinality.ToString(), color: "yellowgreen"));
            
        if (includeActual)
        {
            sb.Append(" (actual ")
                .Append(ExecutionData.ActualCardinality)
                .Append(')');
        }
        
        sb.Append(", S: ")
            .Append(HtmlClasses.Colored(Selectivity.ToString("F5"), color: "olive"));
        
        return sb.ToString();
    }
}
