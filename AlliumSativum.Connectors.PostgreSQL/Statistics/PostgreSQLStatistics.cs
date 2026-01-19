using AlliumSativum.Connectors.PostgreSQL.Models.ORM;
using AlliumSavitum.Connectors.Shared.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AlliumSativum.Connectors.PostgreSQL.Statistics;

public sealed class PostgreSQLStatistics : IDataSourceStatistics
{
    // TODO: replace with some secure keystore !!!
    private Dictionary<string, string> dataSourceConnectionMap = new()
    {
        {"user", "postgresql://admin:admin@localhost:5432/database"}
    };
    
    public async Task ScrapeStatistics(string dataSource)
    {
        if (!dataSourceConnectionMap.TryGetValue(dataSource, out var connectionString))
        {
            throw new ArgumentOutOfRangeException(nameof(dataSource)); 
        }
        
        await using var connection = new SqlConnection(connectionString);
        var tables = await connection.QueryAsync<PostgresTablesModel>("""
                                                                     SELECT table_schema, table_name
                                                                     FROM information_schema.tables
                                                                     WHERE table_schema != "pg_catalog"
                                                                         AND table_schema != "information_schema"
                                                                         AND table_type = "BASE TABLE"
                                                                     """);

        foreach (var table in tables)
        {
            Console.WriteLine(table);
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
