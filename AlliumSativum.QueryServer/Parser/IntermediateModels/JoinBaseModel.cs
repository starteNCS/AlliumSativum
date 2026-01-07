using AlliumSativum.Parser.Constants;
using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser.IntermediateModels;

public sealed class JoinBaseModel
{
    public JoinType JoinType { get; init; }
    public TableSpecifier Outer { get; init; }
    public TableSpecifier Inner { get; init; }
    public IExpressionNode Expression { get; init; }
}

public enum JoinType
{
    FullOuter,
    Left,
    Right,
    Inner
}

public static class JoinTypeExtensions
{
    public static JoinType ToJoinType(this string typeString)
    {
        return typeString.ToUpper() switch
        {
            AsSqlKeywords.JoinType.INNER => JoinType.Inner,
            AsSqlKeywords.JoinType.LEFT => JoinType.Left,
            AsSqlKeywords.JoinType.RIGHT => JoinType.Right,
            AsSqlKeywords.JoinType.OUTER => JoinType.FullOuter,
            _ => throw new ArgumentOutOfRangeException(nameof(typeString), typeString, null)
        };
    }
}