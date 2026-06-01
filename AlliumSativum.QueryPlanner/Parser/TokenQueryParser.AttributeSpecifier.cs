using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser;

public partial class TokenQueryParser
{
    /// <summary>
    ///     Reads the next three tokens as a variable mapping specifier
    /// </summary>
    /// <remarks>
    ///     Variable Mapping Specifier format: "variableName"."attributeName"
    /// </remarks>
    /// <param name="tokens">Stack of tokens</param>
    /// <returns>The variable mapping specifier</returns>
    /// <exception cref="AsSqlParseException">Any of the three tokens did not match the expected format</exception>
    private static ISpecifier GetVariableSpecifier(Stack<string> tokens)
    {
        if (!tokens.TryPop(out var variableName))
            throw new AsSqlParseException($"{variableName}", "expected variable name");

        if (!tokens.TryPop(out var tableNameSeparator) ||
            tableNameSeparator != AsSqlParameters.Attribute.TableSeparator.ToString())
            throw new AsSqlParseException($"{variableName}{tableNameSeparator}",
                $"expected table name separator, got '{tableNameSeparator}'");

        if (!tokens.TryPop(out var attributeName))
            throw new AsSqlParseException($"{variableName}{tableNameSeparator}{attributeName}",
                "expected attribute name");

        // this makes sure no query treats the data source as a variable (i.e. x.y.z instead of x->y.z)
        if (tokens.TryPeek(out var nextToken) && nextToken == AsSqlParameters.Attribute.TableSeparator.ToString())
            throw new AsSqlParseException($"{variableName}{tableNameSeparator}{attributeName}{nextToken}",
                $"invalid table name separator ({AsSqlParameters.Attribute.TableSeparator}), are you sure you didn't mean '{variableName}{AsSqlParameters.Attribute.DataSourceSeparator}{attributeName}{tokens.Pop()}{tokens.Pop()}'?");

        return new VariableMappingSpecifier(variableName, attributeName);
    }

    /// <summary>
    ///     Reads the next five tokens as an attribute specifier
    /// </summary>
    /// <remarks>
    ///     Attribute Specifier format: "datasourceName"->"tableName"."attributeName"
    /// </remarks>
    /// <param name="tokens">Stack of tokens</param>
    /// <returns>The attribute specifier</returns>
    /// <exception cref="AsSqlParseException">Any of the five tokens did not match the expected format</exception>
    private AttributeSpecifier GetAttributeSpecifier(Stack<string> tokens)
    {
        var tableSpecifier = GetTableSpecifier(tokens);

        if (!tokens.TryPop(out var tableNameSeparator) ||
            tableNameSeparator != AsSqlParameters.Attribute.TableSeparator.ToString())
            throw new AsSqlParseException(
                $"{tableSpecifier.DataSourceName}{AsSqlParameters.Attribute.DataSourceSeparator}{tableSpecifier.TableName}{tableNameSeparator}",
                $"expected table name separator, got '{tableNameSeparator}'");

        if (!tokens.TryPop(out var attributeName))
            throw new AsSqlParseException(
                $"{tableSpecifier.DataSourceName}{AsSqlParameters.Attribute.DataSourceSeparator}{tableSpecifier.TableName}{tableNameSeparator}{attributeName}",
                "expected attribute name");

        return tableSpecifier.ToAttributeSpecifier(attributeName);
    }

    /// <summary>
    ///     Reads the next three tokens as a table specifier
    /// </summary>
    /// <remarks>
    ///     Table Specifier format: "datasourceName"->"tableName"
    /// </remarks>
    /// <param name="tokens">Stack of tokens</param>
    /// <returns>The table specifier</returns>
    /// <exception cref="AsSqlParseException">Any of the three tokens did not match the expected format</exception>
    private TableSpecifier GetTableSpecifier(Stack<string> tokens)
    {
        var datasourceSpecifier = GetDataSourceSpecifier(tokens);

        if (!tokens.TryPop(out var datasourceSeparator) ||
            datasourceSeparator != AsSqlParameters.Attribute.DataSourceSeparator)
            throw new AsSqlParseException($"{datasourceSpecifier.DataSourceName}{datasourceSeparator}",
                $"expected datasource separator, got '{datasourceSeparator}'");

        if (!tokens.TryPop(out var tableName))
            throw new AsSqlParseException($"{datasourceSpecifier.DataSourceName}{datasourceSeparator}{tableName}",
                "expected table name");

        return datasourceSpecifier.ToTableSpecifier(tableName);
    }

    /// <summary>
    ///     Reads the next token as a datasource specifier
    /// </summary>
    /// <param name="tokens">Stack of tokens</param>
    /// <returns>The data source specifier</returns>
    /// <exception cref="AsSqlParseException">Token did not match the expected format</exception>
    private static DataSourceSpecifier GetDataSourceSpecifier(Stack<string> tokens)
    {
        return !tokens.TryPop(out var datasource)
            ? throw new AsSqlParseException("", "expected datasource name")
            : new DataSourceSpecifier(datasource);
    }

    /// <summary>
    ///     Returns the top-most attribute-like specifier of the stack;
    ///     May either be a variable mapping specifier or a full attribute specifier
    ///     If the second token is a datasource separator, the top-most specifier is an full attribute specifier, otherwise it
    ///     is a variable mapping specifier
    /// </summary>
    /// <param name="tokens">Stack of tokens</param>
    /// <returns>The specifier</returns>
    /// <exception cref="AsSqlParseException">Some token did not match the expected format</exception>
    private ISpecifier GetVariableOrAttributeSpecifier(Stack<string> tokens)
    {
        var topmost = tokens.Pop();

        if (!tokens.TryPeek(out var variableName))
            throw new AsSqlParseException(topmost,
                "Either an variable mapping or a full attribute specifier needs to be given");

        // we needed to peek into the second-top item. Therefore, we poped the first and now push it again
        tokens.Push(topmost);
        if (variableName == AsSqlParameters.Attribute.DataSourceSeparator) return GetAttributeSpecifier(tokens);

        if (variableName == AsSqlParameters.Attribute.TableSeparator.ToString()) return GetVariableSpecifier(tokens);

        throw new AsSqlParseException($"{topmost} {variableName}",
            $"Expected either an datasource separator ({AsSqlParameters.Attribute.DataSourceSeparator}) or an table separator ({AsSqlParameters.Attribute.TableSeparator})");
    }
}