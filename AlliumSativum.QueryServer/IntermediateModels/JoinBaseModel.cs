namespace AlliumSativum.IntermediateModels;

public sealed class JoinBaseModel
{
    public required JoinType JoinType { get; init; }
    public required TableSpecifier Outer { get; init; }
    public required TableSpecifier Inner { get; init; }
    public required IList<Expression> Expressions { get; init; }
}

public enum JoinType
{
    FullOuter,
    Left,
    Right,
    Inner
}