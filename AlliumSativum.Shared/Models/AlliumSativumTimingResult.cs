namespace AlliumSativum.Shared.Models;

public sealed class AlliumSativumTimingResult
{
    public TimeSpan Tokenize { get; set; }
    public TimeSpan Parse { get; set; }
    public TimeSpan SemanticTransform { get; set; }
    public TimeSpan Optimize { get; set; }
        
    public TimeSpan PlanTotal => Tokenize + Parse + SemanticTransform + Optimize;

    public TimeSpan Execute { get; set; }
}
