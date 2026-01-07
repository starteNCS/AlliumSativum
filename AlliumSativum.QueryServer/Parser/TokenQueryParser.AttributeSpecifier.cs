using AlliumSativum.Parser.Constants;
using AlliumSativum.Parser.Exceptions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser;

public static partial class TokenQueryParser
{
    private static AttributeSpecifier HandleAttributeSpecifier(Stack<string> tokens)
    {
        var tableSpecifier = HandleTableSpecifier(tokens);
        
        if (!tokens.TryPop(out var tableNameSeparator) || tableNameSeparator != AsSqlParameters.Attribute.TableSeparator.ToString())
        {
            throw new AsSqlParseException($"{tableSpecifier.DataSourceName}{AsSqlParameters.Attribute.DataSourceSeparator}{tableSpecifier.TableName}{tableNameSeparator}", $"expected table name separator, got '{tableNameSeparator}'");
        }
        
        if (!tokens.TryPop(out var attributeName))
        {
            throw new AsSqlParseException($"{tableSpecifier.DataSourceName}{AsSqlParameters.Attribute.DataSourceSeparator}{tableSpecifier.TableName}{tableNameSeparator}{attributeName}", "expected attribute name");
        }
        
        return tableSpecifier.ToAttributeSpecifier(attributeName);
    }

    private static TableSpecifier HandleTableSpecifier(Stack<string> tokens)
    {
        var datasourceSpecifier = HandleDataSourceSpecifier(tokens);
        
        if (!tokens.TryPop(out var datasourceSeparator) || datasourceSeparator != AsSqlParameters.Attribute.DataSourceSeparator)
        {
            throw new AsSqlParseException($"{datasourceSpecifier.DataSourceName}{datasourceSeparator}", $"expected datasource separator, got '{datasourceSeparator}'");
        }
        
        if (!tokens.TryPop(out var tableName))
        {
            throw new AsSqlParseException($"{datasourceSpecifier.DataSourceName}{datasourceSeparator}{tableName}", "expected table name");
        }
        
        return datasourceSpecifier.ToTableSpecifier(tableName);
    }

    private static DataSourceSpecifier HandleDataSourceSpecifier(Stack<string> tokens)
    {
        if (!tokens.TryPop(out var datasource))
        {
            throw new AsSqlParseException("", "expected datasource name");
        }
        
        return new DataSourceSpecifier(datasource);
    }
}
