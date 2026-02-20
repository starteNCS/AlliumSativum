using System.Text;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;

namespace AlliumSativum.Shared.Models.ExecutionPlan;

public sealed class QueryExecutionPlan
{
    public required double TotalCost { get; set; }
    public required PlanOperator RootOperator { get; set; }
    
    public required long OptimizeTimeMs { get; set; }

    public string ToPrettyString(bool html = false)
    {
        var stringBuilder = new StringBuilder();

        
        const string costString = $"» TOTAL COST: ";
        var costValueString = TotalCost.ToString("F3");
        const string expectedCardinalityString = $" | EXPECTED CARDINALITY: ";
        const string planTimeMsString = $" | OPTIMIZE TIME: ";
        if (html)
        {
            stringBuilder.Append("<div style=\"font-family: monospace; white-space: pre;\">");
            
            stringBuilder.Append(HtmlClasses.Colored(costString, color: "gray"));
            stringBuilder.Append(costValueString);
            stringBuilder.Append("ms");
            stringBuilder.Append(HtmlClasses.Colored(expectedCardinalityString, color: "gray"));
            stringBuilder.Append(RootOperator.ExpectedCardinality);
            stringBuilder.Append(HtmlClasses.Colored(planTimeMsString, color: "gray"));
            stringBuilder.Append(OptimizeTimeMs);
            stringBuilder.AppendLine("ms");
            
            stringBuilder.Append("<hr />");
            
            stringBuilder.Append("» ")
                .Append(HtmlClasses.Colored(HtmlClasses.Bold("ED"), color: "coral"))
                .AppendLine(": Expected Duration (Cost in Ms)");
            stringBuilder.Append("» ")
                .Append(HtmlClasses.Colored(HtmlClasses.Bold("C "), color: "yellowgreen"))
                .AppendLine(": Cardinality (Estimated number of rows output by the operator)");
            stringBuilder.Append("» ")
                .Append(HtmlClasses.Colored(HtmlClasses.Bold("S "), color: "olive"))
                .AppendLine(": Selectivity (Estimated fraction of rows that pass the operator's filter, between 0 and 1)");
            
            stringBuilder.Append("<hr />");
            
            stringBuilder.Append("</div>");
        }
        else
        {
            stringBuilder.Append(costString);
            stringBuilder.AppendLine(costValueString);
        }

        stringBuilder.Append(RootOperator.ToPrettyString(html));

        return stringBuilder.ToString();
    }
}
