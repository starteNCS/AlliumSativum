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
            var childIsLast = i == Children.Count - 1;
            Children[i].BuildString(sb, childPrefix, childIsLast);
        }
    }

    protected void BuildStringHtml(StringBuilder sb, string prefix, bool isLast, bool includeActual = false)
    {
        sb.Append(prefix);
        sb.Append(isLast ? "└── " : "├── ");
        sb.AppendLine(GetNodeInfoHtml());

        sb.Append(prefix);
        sb.Append(isLast ? "    " : "│   ");
        sb.AppendLine(GetHtmlBaseNodeInfo(includeActual));

        var childPrefix = prefix + (isLast ? "    " : "│   ");
        for (var i = 0; i < Children.Count; i++)
        {
            var childIsLast = i == Children.Count - 1;
            Children[i].BuildStringHtml(sb, childPrefix, childIsLast, includeActual);
        }
    }

    protected abstract string GetNodeInfo();
    protected abstract string GetNodeInfoHtml();

    private string GetBaseNodeInfo()
    {
        return $"ED: {Cost:F2}ms, C: {ExpectedCardinality}, S: {Selectivity}";
    }

    private string GetHtmlBaseNodeInfo(bool includeActual = false)
    {
        var sb = new StringBuilder();
        sb.Append("ED: ")
            .Append(HtmlClasses.Colored(Cost.ToString("F3"), "coral"))
            .Append("ms");
        if (includeActual)
            sb.Append(" (actual ")
                .Append(ExecutionData.ActualCost.ToString("F3"))
                .Append("ms, precision: ")
                .Append(GetTargetPrecisionHtmlString(ExecutionData.ActualCost, Cost))
                .Append(')');
        sb.Append(", C: ")
            .Append(HtmlClasses.Colored(ExpectedCardinality.ToString(), "yellowgreen"));

        if (includeActual)
            sb.Append(" (actual ")
                .Append(ExecutionData.ActualCardinality)
                .Append(')');

        sb.Append(", S: ")
            .Append(HtmlClasses.Colored(Selectivity.ToString("F5"), "olive"));

        if (includeActual)
        {
            sb.Append(" (actual ")
                .Append(GetActualSelectivityInfo().ToString("F5"))
                .Append(')');

            sb.Append(", Selectivity Precision: ")
                .Append(GetTargetPrecisionHtmlString(ExecutionData.ActualCardinality, ExpectedCardinality));
        }

        return sb.ToString();
    }

    protected abstract double GetActualSelectivityInfo();

    private string GetTargetPrecisionHtmlString(double target, double actual)
    {
        var error = GetTargetPrecision(target, actual);
        var color = error switch
        {
            >= 90 => "green",
            >= 70 => "orange",
            _ => "red"
        };
        return HtmlClasses.Colored($"{error:F2}%", color);
    }

    private static double GetTargetPrecision(double target, double actual)
    {
        if (Math.Abs(target) <= 0e-12) return 100;

        return (1 - Math.Abs(target - actual) / target) * 100;
    }
}