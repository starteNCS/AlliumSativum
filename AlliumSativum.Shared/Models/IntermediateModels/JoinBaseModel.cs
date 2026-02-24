using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.IntermediateModels;

public sealed class JoinBaseModel
{
    public JoinType JoinType { get; init; }
    public TableSpecifier Inner { get; init; }
    public ExpressionNode Expression { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not JoinBaseModel join)
        {
            return false;
        }

        return JoinType.Equals(join.JoinType)
               && Inner.Equals(join.Inner)
               && Expression.Equals(join.Expression);
    }
    
    /// <summary>
    /// Returns the "other" table from a join (the table needed for the expression, rather than the newly joined table)
    /// </summary>
    /// <param name="join"></param>
    /// <returns></returns>
    public TableSpecifier GetJoinExpressionTable()
    {
        return Expression.GetAttributesOfExpression()
            .Select(x => new TableSpecifier(x.DataSourceName, x.TableName))
            .Where(x => !x.Equals(Inner))
            .Distinct()
            .Single();
    } 
    
    public List<TableSpecifier> AffectedTables => [Inner, GetJoinExpressionTable()];
}

public enum JoinType
{
    // Outer,
    // Left,
    // Right,
    Inner
}

public static class JoinTypeExtensions
{
    public static JoinType ToJoinType(this string typeString)
    {
        return typeString.ToUpper() switch
        {
            AsSqlKeywords.JoinType.INNER => JoinType.Inner,
            // AsSqlKeywords.JoinType.LEFT => JoinType.Left,
            // AsSqlKeywords.JoinType.RIGHT => JoinType.Right,
            // AsSqlKeywords.JoinType.OUTER => JoinType.Outer,
            _ => throw new ArgumentOutOfRangeException(nameof(typeString), typeString, null)
        };
    }
}