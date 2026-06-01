using System.Text;
using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.IntermediateModels;

/// <summary>
///     DTO for the select statement, containing all necessary information to execute the query
/// </summary>
public sealed class SelectDto
{
    public List<VariableMapping> VariableMappings { get; set; } = [];
    public List<ISpecifier> Select { get; set; } = [];
    public TableSpecifier From { get; set; } = null!;
    public ExpressionNode? Where { get; set; }
    public List<JoinBaseModel> Join { get; set; } = [];

    public List<TableSpecifier> AffectedTables => [From, ..Join.Select(x => x.Inner)];

    public List<AttributeSpecifier> GetAffectedAttributes()
    {
        List<AttributeSpecifier> attributes =
        [
            ..Select.OfType<AttributeSpecifier>(),
            ..Where?.GetAttributesOfExpression() ?? [],
            ..Join.SelectMany(j => j.Expression.GetAttributesOfExpression())
        ];
        return attributes.Distinct().ToList();
    }

    public string ToPostgreSqlString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("SELECT ");
        var first = (AttributeSpecifier)Select[0];
        stringBuilder.Append($"{first.TableName}.{first.AttributeName}");
        AppendAsAttributeString(stringBuilder, first);
        foreach (var specifier in Select.Skip(1))
        {
            if (specifier is not AttributeSpecifier select) continue;

            stringBuilder
                .Append($", {select.TableName}.{select.AttributeName}");
            AppendAsAttributeString(stringBuilder, select);
        }

        stringBuilder.Append($" FROM {From?.TableName}");

        foreach (var join in Join)
        {
            stringBuilder.Append(" INNER JOIN ");
            stringBuilder.Append(join.Inner.TableName);
            stringBuilder.Append(" ON ");
            stringBuilder.Append(join.Expression.ToSqlQueryString());
        }

        if (Where is not null)
        {
            stringBuilder.Append(" WHERE ");
            stringBuilder.Append(Where.ToSqlQueryString());
        }

        return stringBuilder.ToString();
    }

    private static void AppendAsAttributeString(StringBuilder sb, AttributeSpecifier attribute)
    {
        sb.Append(" AS ")
            .Append('"')
            .Append(attribute.DataSourceName)
            .Append(AsSqlParameters.Attribute.DataSourceSeparator)
            .Append(attribute.TableName)
            .Append(AsSqlParameters.Attribute.TableSeparator)
            .Append(attribute.AttributeName)
            .Append('"');
    }

    public override string ToString()
    {
        return ToPostgreSqlString();
    }


    /// <summary>
    ///     Appends the given attributes either as hidden, when not existing
    ///     or leave the attribute as is, when already existing (i.e. not hidden)
    /// </summary>
    /// <param name="hiddenAttributes">The hidden atrtibutes to add</param>
    /// <returns></returns>
    public void AppendHiddenAttribute(List<AttributeSpecifier> hiddenAttributes)
    {
        foreach (var attribute in hiddenAttributes)
        {
            if (Select.Any(s => s is AttributeSpecifier aSpec && aSpec.Equals(attribute)))
                // model already contains specific select
                continue;

            attribute.IsHidden = true;
            Select.Add(attribute);
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is SelectDto model &&
               VariableMappings.SequenceEqual(model.VariableMappings) &&
               Select.SequenceEqual(model.Select) &&
               From.Equals(model.From) &&
               ((Where == null && model.Where == null) || (Where != null && Where.Equals(model.Where))) &&
               Join.SequenceEqual(model.Join);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(VariableMappings, Select, From, Where, Join);
    }
}