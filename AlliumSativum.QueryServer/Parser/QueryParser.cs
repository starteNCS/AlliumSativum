using System.Text.RegularExpressions;
using AlliumSativum.Parser.Constants;
using AlliumSativum.Parser.Exceptions;
using AlliumSativum.Parser.IntermediateModels;
using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser;

public class QueryParser
{
    public SelectBaseModel? Parse(string query)
    {
        query = query.Trim();
        if (!query.StartsWith(SqlKeywords.SELECT))
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
            Where = []
        };
    }

    private static TableSpecifier HandleFrom(string fromQueryPart)
    {
        return HandleTableSpecifier(fromQueryPart);
    }
    
    private static IList<AttributeSpecifier> HandleSelect(string selectQueryPart)
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
        string robustPattern = $@"'[^']*'|\[[^\]]*\]|\([^\)]*\)|(?i)\b({SqlKeywords.SELECT}|{SqlKeywords.FROM}|{SqlKeywords.WHERE}|(LEFT|RIGHT|INNER|FULL\s+OUTER)\s+{SqlKeywords.JOIN})\b";

        var rawSplitQuery = new RawSelectModel();
        var matches = Regex.Matches(query, robustPattern);

        Match? lastKeywordMatch = null;
        foreach (Match match in matches)
        {
            // if Group 1 is success, it means we hit a keyword, not a string/bracket
            if (!match.Groups[1].Success)
            {
                continue;
            }
            if (lastKeywordMatch != null)
            {
                AddPart(lastKeywordMatch, match.Index);
            }
            
            lastKeywordMatch = match;
        }
        
        if (lastKeywordMatch != null)
        {
            AddPart(lastKeywordMatch, query.Length);
        }

        return rawSplitQuery;

        void AddPart(Match match, int nextIndex)
        {
            var key = match.Value.ToUpper();
            var start = match.Index + match.Length;
            rawSplitQuery.Add(key, query.Substring(start, nextIndex - start).Trim());
        }
    }
}