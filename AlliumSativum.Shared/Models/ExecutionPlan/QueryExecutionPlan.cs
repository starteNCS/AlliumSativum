using System.Text;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;

namespace AlliumSativum.Shared.Models.ExecutionPlan;

public sealed class QueryExecutionPlan
{
    public required double TotalCost { get; set; }
    public required PlanOperator RootOperator { get; set; }

    public required long OptimizeTimeMs { get; set; }
    /// <summary>
    /// Flag indicating whether the plan represented by this execution plan was the winning plan after optimization.
    /// </summary>
    public bool OptimizeDidWin { get; set; }

    public string ToPrettyString(bool html = false)
    {
        var stringBuilder = new StringBuilder();


        const string costString = "» TOTAL COST: ";
        var costValueString = TotalCost.ToString("F3");
        const string expectedCardinalityString = " | EXPECTED CARDINALITY: ";
        const string planTimeMsString = " | OPTIMIZE TIME: ";
        if (html)
        {
            stringBuilder.Append("<div style=\"font-family: monospace; white-space: pre;\">");

            stringBuilder.Append(HtmlClasses.Colored(costString, "gray"));
            stringBuilder.Append(costValueString);
            stringBuilder.Append("ms");
            stringBuilder.Append(HtmlClasses.Colored(expectedCardinalityString, "gray"));
            stringBuilder.Append(RootOperator.ExpectedCardinality);
            stringBuilder.Append(HtmlClasses.Colored(planTimeMsString, "gray"));
            stringBuilder.Append(OptimizeTimeMs);
            stringBuilder.AppendLine("ms");

            stringBuilder.Append("<hr />");

            stringBuilder.Append("» ")
                .Append(HtmlClasses.Colored(HtmlClasses.Bold("ED"), "coral"))
                .Append(": Expected Duration ")
                .AppendLine(HtmlClasses.Italic(HtmlClasses.Colored("(Cost in Ms)", "gray")));
            stringBuilder.Append("» ")
                .Append(HtmlClasses.Colored(HtmlClasses.Bold("C "), "yellowgreen"))
                .Append(": Cardinality ")
                .AppendLine(HtmlClasses.Italic(HtmlClasses.Colored("(Estimated number of rows output by the operator)",
                    "gray")));
            stringBuilder.Append("» ")
                .Append(HtmlClasses.Colored(HtmlClasses.Bold("S "), "olive"))
                .Append(": Selectivity ")
                .AppendLine(HtmlClasses.Italic(HtmlClasses.Colored(
                    "(Estimated fraction of rows that pass the operator's filter, between 0 and 1)", "gray")));

            stringBuilder.Append("<hr />");

            stringBuilder.Append("</div>");
        }
        else
        {
            stringBuilder.Append(costString);
            stringBuilder.AppendLine(costValueString);
        }

        stringBuilder.Append(RootOperator.ToPrettyString(html, false));

        return stringBuilder.ToString();
    }
}