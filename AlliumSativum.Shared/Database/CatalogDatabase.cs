using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using Dapper;
using Npgsql;

namespace AlliumSativum.Shared.Database;

public sealed class CatalogDatabase : IDisposable, IAsyncDisposable
{
    private readonly string _connectionString;
    // Only used when a transaction is active
    private NpgsqlConnection? _txConnection;
    private NpgsqlTransaction? _transaction;
    private static SemaphoreSlim _semaphoreSlim = new(1, 99);
    
    public CatalogDatabase(CatalogDatabaseSettings settings)
    {
        _connectionString = settings.ConnectionString;
    }

    public async Task BeginTransactionAsync()
    {
        _txConnection = new NpgsqlConnection(_connectionString);
        await _txConnection.OpenAsync();
        _transaction = await _txConnection.BeginTransactionAsync();
    }
    
    public async Task CommitTransactionAsync() {
        if(_transaction is null) 
        {
            throw new ArgumentException("Transaction cannot be comitted, as no transaction is open");
        }
        
        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
        await _txConnection!.CloseAsync();
        await _txConnection.DisposeAsync();
        _txConnection = null;
    }

    public async Task<List<T>> QueryAsync<T>(string query, object? parameters = null) where T : new()
    {
        await _semaphoreSlim.WaitAsync();
        // If inside a transaction, reuse the dedicated connection
        if (_transaction != null)
        {
            var result = await _txConnection!.QueryAsync<T>(query, parameters, _transaction);
            return result.ToList();
        }
        // Otherwise open a fresh pooled connection (Npgsql pools automatically)
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        var pooledResult = await connection.QueryAsync<T>(query, parameters);
        _semaphoreSlim.Release();
        return pooledResult.ToList();
    }

    public async Task<List<Dictionary<string, object>>> QueryAsync(string query, object? parameters = null) 
    {
        await _semaphoreSlim.WaitAsync();
        // If inside a transaction, reuse the dedicated connection
        if (_transaction != null)
        {
            var result = await _txConnection!.QueryAsync(query, parameters, _transaction);
            return result
                .Select(row => (IDictionary<string, object>)row)
                .Select(dict => dict.ToDictionary(k => k.Key, v => v.Value))
                .ToList();
        }
        // Otherwise open a fresh pooled connection (Npgsql pools automatically)
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        var pooledResult = await connection.QueryAsync(query, parameters);
        _semaphoreSlim.Release();
        return pooledResult
            .Select(row => (IDictionary<string, object>)row)
            .Select(dict => dict.ToDictionary(k => k.Key, v => v.Value))
            .ToList();
    }
    
    public async Task<int> ExecuteAsync(string query, object? parameters = null)
    {
        if (_transaction != null)
        {
            return await _txConnection!.ExecuteAsync(query, parameters, _transaction);
        }
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.ExecuteAsync(query, parameters);
    }
    
    public async Task<DataSourceEntity?> GetDataSourceAsync(Guid dataSource)
    {
        var dataSources = await QueryAsync<DataSourceEntity>($"SELECT * FROM Catalog.DataSources WHERE Id = @DataSourceId",
            new
            {
                DataSourceId = dataSource,
            });
        
        return dataSources.SingleOrDefault();
    }
    
    public async Task<DataSourceEntity?> GetDataSourceAsync(string dataSource)
    {
        var dataSources = await QueryAsync<DataSourceEntity>($"SELECT * FROM Catalog.DataSources WHERE Name = @DataSource",
            new
            {
                DataSource = dataSource,
            });
        
        return dataSources.SingleOrDefault();
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
    
    public async Task<RelationEntity> GetRelationAsync(Guid dataSource, string tableName)
    {
        var relations = await QueryAsync<RelationEntity>($"SELECT * FROM Catalog.Relations WHERE Name LIKE @TableName AND DataSourceId = @DataSourceId",
            new
            {
                TableName = $"%{tableName}",
                DataSourceId = dataSource,
            });
        if (relations.Count > 1)
        {
            throw new ArgumentException(
                $"Datasource '{dataSource}' contains two or more tables with the same name. Please also provide the schema");
        }
        
        return relations.Single();
    }
    
    public async Task<RelationEntity> GetRelationAsync(string dataSource, string tableName)
    {
        var relations = await QueryAsync<RelationEntity>($"""
                                                          SELECT r.* 
                                                          FROM Catalog.Relations r
                                                            JOIN Catalog.DataSources d ON d.Id = r.DataSourceId
                                                          WHERE r.Name LIKE @TableName AND d.Name = @DataSource
                                                          """,
            new
            {
                TableName = $"%{tableName}",
                DataSource = dataSource,
            });
        if (relations.Count > 1)
        {
            throw new ArgumentException(
                $"Datasource '{dataSource}' contains two or more tables with the same name. Please also provide the schema");
        }
        
        return relations.Single();
    }
    
    public async Task<RelationEntity> GetRelationAsync(Guid relationId)
    {
        var relations = await QueryAsync<RelationEntity>($"SELECT * FROM Catalog.Relations WHERE Id = @RelationId",
            new
            {
                RelationId = relationId,
            });
        if (relations.Count > 1)
        {
            throw new ArgumentException(
                $"Relation with id '{relationId}' does not exist or is not unique");
        }
        
        return relations.Single();
    }
    
    public async Task<List<RelationEntity>> GetRelationsOfDataSourceAsync(Guid dataSource)
    {
        var dataSources = await QueryAsync<RelationEntity>($"SELECT * FROM Catalog.Relations WHERE DataSourceId = @DataSourceId",
            new
            {
                DataSourceId = dataSource,
            });
        
        return dataSources;
    }
    
    public async Task<List<AttributeEntity>> GetAttributesOfDataSourceAsync(Guid dataSource)
    {
        var attributes = await QueryAsync<AttributeEntity>($"""
                                                            SELECT a.* 
                                                            FROM Catalog.Attributes a
                                                            JOIN Catalog.Relations r ON r.Id = a.RelationId
                                                            WHERE r.DataSourceId = @DataSourceId
                                                            """,
            new
            {
                DataSourceId = dataSource,
            });
        
        return attributes;
    }

    public async Task<AttributeEntity> GetAttributeAsync(AttributeSpecifier attributeSpecifier)
    {
        var dataSource = await GetDataSourceAsync(attributeSpecifier.DataSourceName);
        if (dataSource is null)
        {
            throw new ArgumentException($"Datasource '{attributeSpecifier.DataSourceName}' not found");
        }
        
        var relation = await GetRelationAsync(dataSource.Id, attributeSpecifier.TableName);
        if (relation is null)
        {
            throw new ArgumentException($"Table '{attributeSpecifier.TableName}' not found in datasource '{attributeSpecifier.DataSourceName}'");
        }
        
        var attribute = await QueryAsync<AttributeEntity>($"SELECT * FROM Catalog.Attributes WHERE Name = @AttributeName AND RelationId = @RelationId",
            new
            {
                AttributeName = attributeSpecifier.AttributeName,
                RelationId = relation.Id,
            });
        if (attribute.Count != 1)
        {
            throw new ArgumentException($"Attribute '{attributeSpecifier.AttributeName}' not found in table '{attributeSpecifier.TableName}' of datasource '{attributeSpecifier.DataSourceName}'");
        }
        
        return  attribute.Single();
    }

    public Task<AttributeEntity> GetAttributeAsync(FullySpecifiedColumnExpressionNode node)
    {
        return GetAttributeAsync(new AttributeSpecifier(node.Attribute.DataSourceName, node.Attribute.TableName, node.Attribute.AttributeName));
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _txConnection?.Close();
        _txConnection?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null) await _transaction.DisposeAsync();
        if (_txConnection != null)
        {
            await _txConnection.CloseAsync();
            await _txConnection.DisposeAsync();
        }
    }
}
