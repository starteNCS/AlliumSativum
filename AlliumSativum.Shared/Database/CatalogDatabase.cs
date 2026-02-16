using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using Dapper;
using Npgsql;

namespace AlliumSativum.Shared.Database;

public sealed class CatalogDatabase : IDisposable, IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private NpgsqlTransaction? _transaction;
    
    
    public CatalogDatabase(CatalogDatabaseSettings settings)
    {
        _connection = new NpgsqlConnection(settings.ConnectionString);
        _connection.Open();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _connection.BeginTransactionAsync();
    }
    
    public async Task CommitTransactionAsync() {
        if(_transaction is null) 
        {
            throw new ArgumentException("Transaction cannot be comitted, as no transaction is open");
        }
        
        await _transaction.CommitAsync();
        _transaction = null;
    }

    public async Task<List<T>> QueryAsync<T>(string query, object? parameters = null) where T : new()
    {
        var result = await _connection.QueryAsync<T>(query, parameters, _transaction);
        return result.ToList();
    }

    public async Task<int> ExecuteAsync(string query, object? parameters = null)
    {
        var result = await _connection.ExecuteAsync(query, parameters, _transaction);
        return result;
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
    
    public async Task<RelationEntity?> GetRelationAsync(Guid dataSource, string tableName)
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
        
        return relations.SingleOrDefault();
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
        _connection.Close();
        _connection.Dispose();
        _transaction?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.CloseAsync();
        await _connection.DisposeAsync();
        if (_transaction != null) await _transaction.DisposeAsync();
    }
}
