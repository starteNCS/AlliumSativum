using AlliumSativum.IntermediateModels.Expressions;

namespace AlliumSativum.IntermediateModels;

public sealed class Expression
{
    public required ValueOrAttributeSpecifier Left { get; init; }
    public required Operator Operator { get; init; }
    public required ValueOrAttributeSpecifier Right { get; init; }
}
