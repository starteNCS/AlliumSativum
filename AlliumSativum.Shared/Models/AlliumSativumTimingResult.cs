namespace AlliumSativum.Shared.Models;

public sealed class AlliumSativumTimingResult
{
    public TimeSpan Tokenize { get; set; }
    public TimeSpan Parse { get; set; }
    public TimeSpan SemanticTransform { get; set; }
    public TimeSpan Optimize { get; set; }

    public TimeSpan PlanningTotal => Tokenize + Parse + SemanticTransform + Optimize;

    public TimeSpan Execute { get; set; }

    public TimeSpan Total => PlanningTotal + Execute;

    public object ToMilliSeconds()
    {
        return new
        {
            Plan = new
            {
                Tokenize = Tokenize.TotalMilliseconds,
                Parse = Parse.TotalMilliseconds,
                SemanticTransform = SemanticTransform.TotalMilliseconds,
                Optimize = Optimize.TotalMilliseconds,
                Total = PlanningTotal.TotalMilliseconds
            },
            Execute = Execute.TotalMilliseconds,
            Total = Total.TotalMilliseconds
        };
    }
}