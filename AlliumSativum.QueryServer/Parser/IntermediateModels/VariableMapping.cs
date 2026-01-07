using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser.IntermediateModels;

public sealed class VariableMapping
{
    public required TableSpecifier Table { get; set; }
    public required string Alias { get; set; }
}