using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser.IntermediateModels;

public sealed class JoinBaseModel
{
    public required JoinType JoinType { get; init; }
    public required TableSpecifier Outer { get; init; }
    public required TableSpecifier Inner { get; init; }
    public required IExpressionNode Expression { get; init; }
}

public enum JoinType
{
    FullOuter,
    Left,
    Right,
    Inner
}