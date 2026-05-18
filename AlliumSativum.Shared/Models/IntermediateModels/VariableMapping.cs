using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.IntermediateModels;

/// <summary>
/// Storing the mapping between a variable and the table it represents.
/// Used exclusively in the semantic transformation
/// </summary>
public sealed class VariableMapping
{
    public required TableSpecifier Table { get; set; }
    public required string Alias { get; set; }
}