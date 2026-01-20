using AlliumSativum.Connectors.PostgreSQL.Models.ORM;
using AlliumSavitum.Connectors.Shared.Interfaces;
using Dapper;
using Dapper.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AlliumSativum.Connectors.PostgreSQL.Statistics;

public sealed class PostgreSqlStatistics : IDataSourceStatistics
{
    private readonly IDapper _dapper;
    private readonly ILogger<PostgreSqlStatistics> _logger;

    public PostgreSqlStatistics(
        IDapper dapper,
        ILogger<PostgreSqlStatistics> logger)
    {
        _dapper = dapper;
        _logger = logger;
    }
    
    public async Task ScrapeStatistics(string dataSource)
    {
        var tables = await _dapper.QueryAsync<PostgresTablesModel>("""
                                                                   SELECT table_schema AS TableSchema, table_name AS TableName
                                                                   FROM information_schema.tables
                                                                   WHERE table_schema != 'pg_catalog'
                                                                       AND table_schema != 'information_schema'
                                                                       AND table_type = 'BASE TABLE'
                                                                   """);
        var columnsParameters = new
        {
            TableNames = tables.Select(t => t.TableName).ToArray(),
            TableSchemas = tables.Select(t => t.TableSchema).Distinct().ToArray(),
        };
        var columns = await _dapper.QueryAsync<PostgresColumnsModel>("""
                                               SELECT table_schema AS TableSchema, table_name AS TableName, column_name AS ColumnName, data_type AS DataType, character_octet_length AS MaximumOctetLength
                                               FROM information_schema.columns
                                               WHERE table_name = ANY(@TableNames)
                                                   AND table_schema = ANY(@TableSchemas)
                                               """, columnsParameters);
        
        _logger.LogInformation("Scrape statistics -> {columns} columns over {tables} tables", columns.Count, tables.Count);
        foreach (var column in columns)
        {
            Console.WriteLine($"{column.TableName}.{column.ColumnName}, {column.DataType}, {column.MaximumOctetLength}");
        }
    }

    public double GetCardinalityOfTable(string dataSource, string table)
    {
        throw new NotImplementedException();
    }

    public double GetUpperBoundSizeOfTable(string dataSource, string table)
    {
        throw new NotImplementedException();
    }

    public double GetUpperBoundSizeOfTable(string dataSource, string table, List<string> columns)
    {
        throw new NotImplementedException();
    }
}
