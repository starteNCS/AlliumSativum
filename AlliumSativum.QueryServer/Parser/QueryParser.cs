using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using AlliumSativum.Parser.Algorithms;
using AlliumSativum.Parser.Constants;
using AlliumSativum.Parser.Exceptions;
using AlliumSativum.Parser.IntermediateModels;
using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser;

public class QueryParser
{
    public SelectBaseModel? Parse(string query)
    {
        query = query.Trim();
        if (!query.StartsWith(AsSqlKeywords.SELECT))
        {
            throw new ArgumentException("Query must start with 'SELECT'", nameof(query));
        }

        var rawQuery = SplitQuery(query);
        if (!rawQuery.Validate())
        {
            return null;
        }
        
        return new SelectBaseModel()
        {
            Select = HandleSelect(rawQuery.Select!),
            From = HandleFrom(rawQuery.From!),
            Join = [],
            Where = HandleWhere(rawQuery.Where)
        };
    }

    private static IExpressionNode? HandleWhere(string? whereQuery)
    {
        return whereQuery == null ? null : BooleanExpressionParser.Parse(whereQuery);
    }

    private static TableSpecifier HandleFrom(string fromQueryPart)
    {
        return HandleTableSpecifier(fromQueryPart);
    }
    
    private static List<AttributeSpecifier> HandleSelect(string selectQueryPart)
    {
        var attributes = selectQueryPart.Split(AsSqlParameters.Attribute.FieldDelimiter);

        return attributes.Select(HandleAttributeSpecifier).ToList();
    }

    private static TableSpecifier HandleTableSpecifier(string tableName)
    {
        tableName = tableName.Trim();
        var dataSourceTemp = tableName.Split(AsSqlParameters.Attribute.DataSourceSeparator);
        if (dataSourceTemp.Length != 2)
        {
            throw new AsSqlParseException(tableName,
                $"Could not find datasource separator ({AsSqlParameters.Attribute.DataSourceSeparator})");
        }
        return new TableSpecifier(dataSourceTemp[0], dataSourceTemp[1]);
    }
    
    private static AttributeSpecifier HandleAttributeSpecifier(string fieldName)
    {
        fieldName = fieldName.Trim();
        var dataSourceTemp = fieldName.Split(AsSqlParameters.Attribute.DataSourceSeparator);
        if (dataSourceTemp.Length != 2)
        {
            throw new AsSqlParseException(fieldName,
                $"Could not find datasource separator ({AsSqlParameters.Attribute.DataSourceSeparator})");
        }
        var attributeTemp = dataSourceTemp[1].Split(AsSqlParameters.Attribute.TableSeparator);
        if (attributeTemp.Length != 2)
        {
            throw new AsSqlParseException(fieldName,
                $"Invalid number of attribute separators (expected 2, found {attributeTemp.Length})");
        }
        return new AttributeSpecifier(dataSourceTemp[0], attributeTemp[0], attributeTemp[1]);
    }
    
    private static RawSelectModel SplitQuery(string query)
    {
        const string pattern = $@"'[^']*'|\[[^\]]*\]|\([^\)]*\)|(?i)\b({AsSqlKeywords.SELECT}|{AsSqlKeywords.FROM}|{AsSqlKeywords.WHERE}|(LEFT|RIGHT|INNER|FULL\s+OUTER)\s+{AsSqlKeywords.JOIN})\b";

        var rawSplitQuery = new RawSelectModel();
        var matches = Regex.Matches(query, pattern);

        Match? lastKeywordMatch = null;
        foreach (Match match in matches)
        {
            // everything in group 0 is within quotes (or brackets)
            if (!match.Groups[1].Success)
            {
                continue;
            }
            if (lastKeywordMatch != null)
            {
                rawSplitQuery.Add(lastKeywordMatch.Value.ToUpper(), GetMatchContent(lastKeywordMatch, query, match.Index));
            }
            
            lastKeywordMatch = match;
        }
        
        if (lastKeywordMatch != null)
        {
            rawSplitQuery.Add(lastKeywordMatch.Value.ToUpper(), GetMatchContent(lastKeywordMatch, query, query.Length));
        }

        return rawSplitQuery;
    }

    private static string GetMatchContent(Match match, string fulltext, int nextIndex)
    {
        var start = match.Index + match.Length;
        return fulltext.Substring(start, nextIndex - start).Trim();
    }
}