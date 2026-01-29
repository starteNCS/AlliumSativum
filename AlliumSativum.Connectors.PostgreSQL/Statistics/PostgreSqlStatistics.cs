using System.Diagnostics;
using System.Text;
using AlliumSativum.Connectors.PostgreSQL.DatabaseConnectors;
using AlliumSativum.Connectors.PostgreSQL.Models.ORM;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Database.Entities;
using AlliumSavitum.Connectors.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace AlliumSativum.Connectors.PostgreSQL.Statistics;

public sealed class PostgreSqlStatistics : IDataSourceStatistics
{
    private readonly CatalogDatabase _catalogDatabase;
    private readonly DatasourceDatabase _dataSource;
    private readonly ILogger<PostgreSqlStatistics> _logger;

    public PostgreSqlStatistics(
        CatalogDatabase catalog,
        DatasourceDatabase dataSource,
        ILogger<PostgreSqlStatistics> logger)
    {
        _catalogDatabase = catalog;
        _dataSource = dataSource;
        _logger = logger;
    }
    
    public async Task ScrapeStatistics(Guid dataSource)
    {
        var stopwatch = Stopwatch.StartNew();
        var tables = await _dataSource.QueryAsync<PostgresTablesModel>(dataSource, "SELECT table_schema AS TableSchema, table_name AS TableName FROM information_schema.tables WHERE table_schema != 'pg_catalog' AND table_schema != 'information_schema' AND table_type = 'BASE TABLE'");

        if (tables.Count == 0)
        {
            _logger.LogInformation("Stop scraping metrics, as no tables were found.");
            return;
        }
        
        var columnsParameters = new
        {
            TableNames = tables.Select(t => t.TableName).ToArray(),
            TableSchemas = tables.Select(t => t.TableSchema).Distinct().ToArray(),
        };
        var columns = await _dataSource.QueryAsync<PostgresColumnsModel>(dataSource, """
                                                                         SELECT table_schema AS TableSchema, table_name AS TableName, column_name AS ColumnName, data_type AS DataType, character_octet_length AS MaximumOctetLength
                                                                         FROM information_schema.columns
                                                                         WHERE table_name = ANY(@TableNames)
                                                                             AND table_schema = ANY(@TableSchemas)
                                                                         """, columnsParameters);

        var existingRelations = await _catalogDatabase.QueryAsync<RelationEntity>("SELECT * FROM Catalog.Relations");
        var existingAttributes = await _catalogDatabase.QueryAsync<AttributeEntity>("SELECT * FROM Catalog.Attributes");

        var relationMetrics = new List<RelationEntity>();
        var attributeMetrics = new List<AttributeEntity>();
        
        foreach (var table in tables)
        {
            var (relMetrics, attrMetrics) = await GetTableMetricsAsync(dataSource, columns, table, existingRelations, existingAttributes);
            relationMetrics.AddRange(relMetrics);
            attributeMetrics.AddRange(attrMetrics);
        }
        
        await _catalogDatabase.ExecuteAsync("""
                                            INSERT INTO Catalog.Relations (Id, DataSourceId, Name, Cardinality, MetricsDate, ConnectionOpenMs, Transfer100Ms)
                                            VALUES (@Id, @DataSourceId, @Name, @Cardinality, @MetricsDate, @ConnectionOpenMs, @Transfer100Ms)
                                            ON CONFLICT (Id)
                                            DO UPDATE SET 
                                                          Cardinality = EXCLUDED.Cardinality,
                                                          MetricsDate = EXCLUDED.MetricsDate,
                                                          ConnectionOpenMs = EXCLUDED.ConnectionOpenMs,
                                                          Transfer100Ms = EXCLUDED.Transfer100Ms
                                            """,  relationMetrics);
        
        await _catalogDatabase.ExecuteAsync("""
                                            INSERT INTO Catalog.Attributes (Id, RelationId, Name, DistinctCardinality, MetricsDate)
                                            VALUES (@Id, @RelationId, @Name, @DistinctCardinality, @MetricsDate)
                                            ON CONFLICT (Id)
                                            DO UPDATE SET 
                                                          DistinctCardinality = EXCLUDED.DistinctCardinality,
                                                          MetricsDate = EXCLUDED.MetricsDate
                                            """,  attributeMetrics);
        
        stopwatch.Stop();
        _logger.LogInformation("Scrape statistics for {DataSource} took {StopwatchElapsedMilliseconds}ms", dataSource, stopwatch.ElapsedMilliseconds);
    }

    private async Task<(List<RelationEntity> relationEntities, List<AttributeEntity> attributeEntities)> GetTableMetricsAsync(
        Guid dataSource, 
        IList<PostgresColumnsModel> columns, 
        PostgresTablesModel table,
        List<RelationEntity> existingRelations, 
        List<AttributeEntity> existingAttributes)
    {
        var relationMetrics = new List<RelationEntity>();
        var attributeMetrics = new List<AttributeEntity>();

        var warumUp = await _dataSource.TimeQueryAsync(dataSource, $"SELECT 1");
        
        var accessTime = await _dataSource.TimeQueryAsync(dataSource, $"SELECT * FROM {table.TableSchema}.{table.TableName} LIMIT 1");
        var transfer100 = await _dataSource.TimeQueryAsync(dataSource, $"SELECT * FROM {table.TableSchema}.{table.TableName} LIMIT 100");
        
        // SQL Injection?
        var tableStatsStringBuilder = new StringBuilder();
        tableStatsStringBuilder.Append("SELECT COUNT(*) AS Total");
        foreach (var column in columns.Where(c =>
                     c.TableName == table.TableName && c.TableSchema == table.TableSchema).Select(t => t.ColumnName))
        {
            tableStatsStringBuilder.Append($", COUNT(DISTINCT {column}) AS {column}");
        }
        tableStatsStringBuilder.Append($" FROM {table.TableSchema}.{table.TableName}");
            
        var tableStats = await _dataSource.QueryAsync<dynamic>(dataSource, tableStatsStringBuilder.ToString());
        foreach (var row in tableStats)
        {
            var rowDict = (IDictionary<string, object>)row;
            var relationId = existingRelations.Find(x => x.Name == $"{table.TableSchema}.{table.TableName}")?.Id ?? Guid.NewGuid();
                
            relationMetrics.Add(new RelationEntity
            {
                Id = relationId,
                DataSourceId = dataSource,
                Name = $"{table.TableSchema}.{table.TableName}",
                MetricsDate = DateTime.Now,
                Cardinality = (long)rowDict["total"],
                ConnectionOpenMs = accessTime,
                Transfer100Ms = transfer100
            });

            foreach (var column in rowDict.Keys.Where(r => r != "total"))
            {
                var attributeId = existingAttributes.Find(x => x.RelationId == relationId && x.Name == column)?.Id ?? Guid.NewGuid();
                attributeMetrics.Add(new AttributeEntity
                {
                    Id = attributeId,
                    RelationId = relationId, 
                    Name = column,
                    MetricsDate = DateTime.Now,
                    DistinctCardinality = (long) rowDict[column] 
                });
            }
        }
        
        return (relationMetrics, attributeMetrics);
    }

    public double GetCardinalityOfTable(Guid dataSource, string table)
    {
        throw new NotImplementedException();
    }

    public double GetUpperBoundSizeOfTable(Guid dataSource, string table)
    {
        throw new NotImplementedException();
    }

    public double GetUpperBoundSizeOfTable(Guid dataSource, string table, List<string> columns)
    {
        throw new NotImplementedException();
    }
}
