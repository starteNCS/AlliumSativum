using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser.IntermediateModels.Expressions;

public sealed class Expression
{
    public required ValueOrAttributeSpecifier Left { get; init; }
    public required Operator Operator { get; init; }
    public required ValueOrAttributeSpecifier Right { get; init; }
}
