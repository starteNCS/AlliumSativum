using AlliumSativum.Shared.Database.Entities;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AlliumSativum.Shared.Database;

public sealed class CatalogDatabase
{
    private readonly string _connectionString;

    public CatalogDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<T>> QueryAsync<T>(string query, object? parameters = null) where T : new()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var result = await connection.QueryAsync<T>(query, parameters);
        return result.ToList();
    }

    public async Task<int> ExecuteAsync(string query, object? parameters = null)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var result = await connection.ExecuteAsync(query, parameters);
        return result;
    }

    public async Task<RelationEntity?> GetRelationAsync(Guid dataSource, string schemaName, string tableName)
    {
        var relations = await QueryAsync<RelationEntity>($"SELECT * FROM Catalog.Relations WHERE Name = @TableName AND DataSourceId = @DataSourceId",
            new
            {
                TableName = $"{schemaName}.{tableName}",
                DataSourceId = dataSource,
            });
        return relations.SingleOrDefault();
    }
    
    public async Task<RelationEntity?> GetRelationAsync(Guid dataSource, string tableName)
    {
        var relations = await QueryAsync<RelationEntity>($"SELECT * FROM Catalog.Relations WHERE Name LIKE @TableName AND DataSourceId = @DataSourceId",
            new
            {
                TableName = $"%.{tableName}",
                DataSourceId = dataSource,
            });
        if (relations.Count > 1)
        {
            throw new ArgumentException(
                $"Datasource '{dataSource}' contains two or more tables with the same name. Please also provide the schema");
        }
        
        return relations.SingleOrDefault();
    }
}
