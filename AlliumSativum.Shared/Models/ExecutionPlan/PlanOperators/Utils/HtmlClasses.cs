namespace AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Utils;

public static class HtmlClasses
{
    public static string Colored(string? value, string color = "blue")
    {
        return $"<span style=\"color: {color};\">{value}</span>";
    }
    
    public static string Bold(string? value)
    {
        return $"<b>{value}</b>";
    }
    
    public static string Italic(string? value)
    {
        return $"<i>{value}</i>";
    }
}
