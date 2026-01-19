using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.IntermediateModels;

public sealed class VariableMapping
{
    public required TableSpecifier Table { get; set; }
    public required string Alias { get; set; }
}