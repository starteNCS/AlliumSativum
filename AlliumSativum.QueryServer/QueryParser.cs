using AlliumSativum.Constants;
using AlliumSativum.Exceptions;
using AlliumSativum.IntermediateModels;

namespace AlliumSativum;

public class QueryParser
{
    public SelectBaseModel Parse(string query)
    {
        query = query.Trim();
        if (!query.StartsWith(SqlKeywords.SELECT))
        {
            throw new ArgumentException("Query must start with 'SELECT'", nameof(query));
        }

        var (select, from) = SplitQuery(query);
        return new SelectBaseModel()
        {
            Select = HandleSelect(select),
            From = HandleFrom(from),
            Join = [],
            Where = []
        };
    }

    private static TableSpecifier HandleFrom(string fromQueryPart)
    {
        fromQueryPart = fromQueryPart.Trim();
        if (!fromQueryPart.StartsWith(SqlKeywords.FROM))
        {
            throw new ArgumentException($"Query part must start with '{SqlKeywords.FROM}'", nameof(fromQueryPart));
        }
        
        fromQueryPart = fromQueryPart.Remove(0, SqlKeywords.FROM.Length).Trim();
        return HandleTableSpecifier(fromQueryPart);
    }
    
    private static IList<AttributeSpecifier> HandleSelect(string selectQueryPart)
    {
        selectQueryPart = selectQueryPart.Trim();
        if (!selectQueryPart.StartsWith(SqlKeywords.SELECT))
        {
            throw new ArgumentException($"Query part must start with '{SqlKeywords.FROM}'", nameof(selectQueryPart));
        }
        
        selectQueryPart = selectQueryPart.Remove(0, SqlKeywords.SELECT.Length).Trim();
        var attributes = selectQueryPart.Split(AsSQLParameters.Attribute.FieldDelimiter);

        return attributes.Select(HandleAttributeSpecifier).ToList();
    }

    private static TableSpecifier HandleTableSpecifier(string tableName)
    {
        tableName = tableName.Trim();
        var dataSourceTemp = tableName.Split(AsSQLParameters.Attribute.DataSourceSeparator);
        if (dataSourceTemp.Length != 2)
        {
            throw new AsSqlParseException(tableName,
                $"Could not find datasource separator ({AsSQLParameters.Attribute.DataSourceSeparator})");
        }
        return new TableSpecifier(dataSourceTemp[0], dataSourceTemp[1]);
    }
    
    private static AttributeSpecifier HandleAttributeSpecifier(string fieldName)
    {
        fieldName = fieldName.Trim();
        var dataSourceTemp = fieldName.Split(AsSQLParameters.Attribute.DataSourceSeparator);
        if (dataSourceTemp.Length != 2)
        {
            throw new AsSqlParseException(fieldName,
                $"Could not find datasource separator ({AsSQLParameters.Attribute.DataSourceSeparator})");
        }
        var attributeTemp = dataSourceTemp[1].Split(AsSQLParameters.Attribute.TableSeparator);
        if (attributeTemp.Length != 2)
        {
            throw new AsSqlParseException(fieldName,
                $"Invalid number of attribute separators (expected 2, found {attributeTemp.Length})");
        }
        return new AttributeSpecifier(dataSourceTemp[0], attributeTemp[0], attributeTemp[1]);
    }
    
    private static (string select, string from) SplitQuery(string query)
    {
        var temp = query.Split("FROM");
        return (temp[0], "FROM" + temp[1]);
    }
}