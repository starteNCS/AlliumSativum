using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Enums;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AlliumSativum.Connectors.PostgreSQL.DatabaseConnectors;

    public sealed class DatasourceDatabase
{
    private static Dictionary<Guid, string> _sConnections = new ();
    
    
    private readonly CatalogDatabase _catalogDatabase;
    private readonly ILogger<DatasourceDatabase> _logger;

    public DatasourceDatabase(CatalogDatabase catalogDatabase, ILogger<DatasourceDatabase> logger)
    {
        _catalogDatabase = catalogDatabase;
        _logger = logger;
    }

    public async Task<IList<T>> QueryAsync<T>(Guid dataSource, string query, object? parameters = null)
        where T : new()
    {
        var connectionString = await GetConnectionStringForDataSource(dataSource);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string may not be null");
        }

        _logger.LogDebug("Executing query against {DataSource}: {Query}", dataSource, query);
        try 
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(); 
            var result = await connection.QueryAsync<T>(query, parameters);
            await connection.CloseAsync();
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query datasource {DataSource}", dataSource);
            return [];
        }
    }

    public async Task<int> ExecuteAsync(Guid dataSource, string query, object? parameters = null)
    {
        var connectionString = await GetConnectionStringForDataSource(dataSource);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string may not be null");
        }

        _logger.LogDebug("Executing query against {DataSource}: {Query}", dataSource, query);
        try 
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(); 
            var result = await connection.ExecuteAsync(query, parameters);
            await connection.CloseAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query datasource {DataSource}", dataSource);
            return -1;
        }
    }

    private async Task<string?> GetConnectionStringForDataSource(Guid dataSource) 
    {
        if (!_sConnections.TryGetValue(dataSource, out var connectionString))
        {
            var dataSources = await _catalogDatabase.QueryAsync<DataSourceConnectionStringModel>(
                "SELECT ConnectionString, Connector FROM Catalog.DataSources WHERE Id = @DataSource FETCH FIRST 1 ROW ONLY", 
                new { DataSource = dataSource }
            );
            var c = dataSources.FirstOrDefault();
            if (c is not { Connector: ConnectorType.Postgres })
            {
                _logger.LogError("DataSource '{DataSource}' is misconfigured", dataSource);
                return null;
            }
            
            _sConnections[dataSource] = c.ConnectionString;
            connectionString = c.ConnectionString;
        }

        return connectionString;
    }
}

internal class DataSourceConnectionStringModel
{
    public string ConnectionString { get; set; } = string.Empty;
    public ConnectorType Connector { get; set; }
}
