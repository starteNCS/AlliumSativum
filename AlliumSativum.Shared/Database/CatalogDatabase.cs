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
}
