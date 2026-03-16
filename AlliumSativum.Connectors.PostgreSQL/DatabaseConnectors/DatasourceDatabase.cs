using System.Diagnostics;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Enums;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AlliumSativum.Connectors.PostgreSQL.DatabaseConnectors;

public sealed class DatasourceDatabase
{
    private const string ConnectionStringEmptyErrorMessage = "Connection string may not be null";
    private static readonly SemaphoreSlim _semaphoreSlim = new(1, 99);
    private static readonly Dictionary<Guid, string> _sConnections = new();


    private readonly CatalogDatabase _catalogDatabase;
    private readonly ILogger<DatasourceDatabase> _logger;

    public DatasourceDatabase(CatalogDatabase catalogDatabase, ILogger<DatasourceDatabase> logger)
    {
        _catalogDatabase = catalogDatabase;
        _logger = logger;
    }

    public async Task<List<T>> QueryAsync<T>(Guid dataSource, string query, object? parameters = null)
    {
        var connection = await GetConnectionStringForDataSource(dataSource);
        if (connection is null) throw new ArgumentException("");

        _logger.LogDebug("Executing query against {DataSource}: {Query}", dataSource, query);
        try
        {
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
        finally
        {
            await connection.DisposeAsync();
        }
    }

    public async Task<List<Dictionary<string, object>>> QueryAsync(Guid dataSource, string query,
        object? parameters = null)
    {
        var connection = await GetConnectionStringForDataSource(dataSource);
        if (connection is null) throw new ArgumentException(ConnectionStringEmptyErrorMessage);

        _logger.LogDebug("Executing query against {DataSource}: {Query}", dataSource, query);
        try
        {
            await connection.OpenAsync();
            var result = await connection.QueryAsync(query, parameters);
            await connection.CloseAsync();
            return result
                .Select(row => (IDictionary<string, object>)row)
                .Select(dict => dict.ToDictionary(k => k.Key, v => v.Value))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query datasource {DataSource}", dataSource);
            return [];
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    public async Task<long> TimeQueryAsync(Guid dataSource, string query, object? parameters = null)
    {
        var connection = await GetConnectionStringForDataSource(dataSource);
        if (connection is null) throw new ArgumentException(ConnectionStringEmptyErrorMessage);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            await connection.OpenAsync();
            var _ = await connection.QueryAsync<dynamic>(query, parameters);
            await connection.CloseAsync();
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query against datasource {DataSource}", dataSource);
            return -1;
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    public async Task<int> ExecuteAsync(Guid dataSource, string query, object? parameters = null)
    {
        var connection = await GetConnectionStringForDataSource(dataSource);
        if (connection is null) throw new ArgumentException(ConnectionStringEmptyErrorMessage);

        _logger.LogDebug("Executing query against {DataSource}: {Query}", dataSource, query);
        try
        {
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
        finally
        {
            await connection.DisposeAsync();
        }
    }

    private async Task<NpgsqlConnection?> GetConnectionStringForDataSource(Guid dataSource)
    {
        await _semaphoreSlim.WaitAsync();
        try
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

            return new NpgsqlConnection(connectionString);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
}

internal class DataSourceConnectionStringModel
{
    public string ConnectionString { get; set; } = string.Empty;
    public ConnectorType Connector { get; set; }
}