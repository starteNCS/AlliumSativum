using System.Text;
using AlliumSativum.Connectors.PostgreSQL.DatabaseConnectors;
using AlliumSativum.Connectors.PostgreSQL.Models.ORM;
using AlliumSativum.Connectors.Shared;
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

    private readonly SemaphoreSlim _semaphore = new(1, 1);

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
        var tables = await _dataSource.QueryAsync<PostgresTablesModel>(dataSource,
            "SELECT table_schema AS TableSchema, table_name AS TableName FROM information_schema.tables WHERE table_schema != 'pg_catalog' AND table_schema != 'information_schema' AND table_type = 'BASE TABLE'");

        if (tables.Count == 0)
        {
            _logger.LogInformation("Stop scraping metrics, as no tables were found.");
            return;
        }

        var columnsParameters = new
        {
            TableNames = tables.Select(t => t.TableName).ToArray(),
            TableSchemas = tables.Select(t => t.TableSchema).Distinct().ToArray()
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

        var tasks = tables.Select(table =>
            GetTableMetricsAsync(dataSource, columns, table, existingRelations, existingAttributes));
        var results = await Task.WhenAll(tasks);
        foreach (var (relMetrics, attrMetrics) in results)
        {
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
                                            """, relationMetrics);

        await _catalogDatabase.ExecuteAsync("""
                                            INSERT INTO Catalog.Attributes (Id, RelationId, Name, DistinctCardinality, MetricsDate, Min, Max, DataType, Mean, MeanBinHeight, Range, Variance, StandardDeviation, Skewness, Kurtosis, DistributionType)
                                            VALUES (@Id, @RelationId, @Name, @DistinctCardinality, @MetricsDate, @Min, @Max, @DataType, @Mean, @MeanBinHeight, @Range, @Variance, @StandardDeviation, @Skewness, @Kurtosis, @DistributionType)
                                            ON CONFLICT (Id)
                                            DO UPDATE SET 
                                                          DistinctCardinality = EXCLUDED.DistinctCardinality,
                                                          MetricsDate = EXCLUDED.MetricsDate,
                                                          Min = EXCLUDED.Min,
                                                          Max = EXCLUDED.Max,
                                                          DataType = EXCLUDED.DataType,
                                                          Mean = EXCLUDED.Mean,
                                                          MeanBinHeight = EXCLUDED.MeanBinHeight,
                                                          Range = EXCLUDED.Range,
                                                          Variance = EXCLUDED.Variance,
                                                          StandardDeviation = EXCLUDED.StandardDeviation,
                                                          Skewness = EXCLUDED.Skewness,
                                                          Kurtosis = EXCLUDED.Kurtosis,
                                                          DistributionType = EXCLUDED.DistributionType
                                            """, attributeMetrics);
    }

    private async Task<(List<RelationEntity> relationEntities, List<AttributeEntity> attributeEntities)>
        GetTableMetricsAsync(
            Guid dataSource,
            IList<PostgresColumnsModel> columns,
            PostgresTablesModel table,
            List<RelationEntity> existingRelations,
            List<AttributeEntity> existingAttributes)
    {
        _logger.LogInformation("Start scraping metrics for table {TableSchema}.{TableName}", table.TableSchema,
            table.TableName);
        var relationMetrics = new List<RelationEntity>();
        var attributeMetrics = new List<AttributeEntity>();

        var warumUp = await _dataSource.TimeQueryAsync(dataSource, "SELECT 1");

        var accessTime = await _dataSource.TimeQueryAsync(dataSource,
            $"SELECT * FROM {table.TableSchema}.{table.TableName} LIMIT 1");
        var transfer100 = await _dataSource.TimeQueryAsync(dataSource,
            $"SELECT * FROM {table.TableSchema}.{table.TableName} LIMIT 100");

        // SQL Injection?
        var tableStatsStringBuilder = new StringBuilder();
        tableStatsStringBuilder.Append("SELECT COUNT(*) AS Total");
        foreach (var column in columns.Where(c =>
                     c.TableName == table.TableName && c.TableSchema == table.TableSchema))
        {
            tableStatsStringBuilder.Append($", COUNT(DISTINCT {column.ColumnName}) AS {column.ColumnName}_distinct");
            if (column.IsNummeric)
                tableStatsStringBuilder.Append(
                    $", MIN({column.ColumnName}) AS {column.ColumnName}_min, MAX({column.ColumnName}) AS {column.ColumnName}_max");
        }

        tableStatsStringBuilder.Append($" FROM {table.TableSchema}.{table.TableName}");

        var tableStats = await _dataSource.QueryAsync<dynamic>(dataSource, tableStatsStringBuilder.ToString());
        foreach (var row in tableStats)
        {
            var rowDict = (IDictionary<string, object>)row;
            var relationId = existingRelations.Find(x => x.Name == $"{table.TableSchema}.{table.TableName}")?.Id ??
                             Guid.NewGuid();

            relationMetrics.Add(new RelationEntity
            {
                Id = relationId,
                DataSourceId = dataSource,
                Name = $"{table.TableSchema}.{table.TableName}",
                MetricsDate = DateTime.Now,
                Cardinality = (long)rowDict["total"],
                ConnectionOpenMs = accessTime,
                Transfer100Ms =
                    Math.Max(transfer100 - accessTime,
                        1) // max 1, as 0 would make no sense in multiplication used later for cost
            });

            foreach (var column in columns.Where(x =>
                         x.TableSchema == table.TableSchema && x.TableName == table.TableName))
            {
                var attribute = existingAttributes.Find(x => x.RelationId == relationId && x.Name == column.ColumnName);
                var attributeId = attribute?.Id ?? Guid.NewGuid();
                double? min = null;
                double? max = null;
                if (rowDict.TryGetValue($"{column.ColumnName}_min", out var minObj)) min = Convert.ToDouble(minObj);
                if (rowDict.TryGetValue($"{column.ColumnName}_max", out var maxObj)) max = Convert.ToDouble(maxObj);

                var attributeEntity = new AttributeEntity
                {
                    Id = attributeId,
                    RelationId = relationId,
                    Name = column.ColumnName,
                    MetricsDate = DateTime.Now,
                    DistinctCardinality = (long)rowDict[$"{column.ColumnName}_distinct"],
                    Min = min,
                    Max = max,
                    DataType = column.DataType
                };
                List<AttributePeakEntity> modes = [];

                var data = await _dataSource.QueryAsync(dataSource,
                    $"SELECT {attributeEntity.Name} FROM {table.TableSchema}.{table.TableName}");
                if (attributeEntity.IsNumeric)
                {
                    var items = data
                        .Select(double? (x) =>
                            x.TryGetValue(attributeEntity.Name, out var value) ? Convert.ToDouble(value) : null)
                        .ToList();
                    (attributeEntity, modes) = DistributionUtils.CalculateDistribution(items, attributeEntity);
                }
                else
                {
                    var items = data
                        .Select(x => Convert.ToString(x.GetValueOrDefault(attributeEntity.Name)) ?? string.Empty)
                        .ToList();
                    (attributeEntity, modes) = DistributionUtils.CalculateDistribution(items, attributeEntity);
                }

                await _semaphore.WaitAsync();
                try
                {
                    attributeMetrics.Add(attributeEntity);
                    _logger.LogWarning("Get lock for {Relation} {Attribute}", relationId, attributeEntity.Name);
                    await _catalogDatabase.ExecuteAsync(
                        "DELETE FROM Catalog.AttributePeaks WHERE AttributeId = @AttributeId",
                        new { AttributeId = attributeId });
                    foreach (var mode in modes)
                        await _catalogDatabase.ExecuteAsync("""
                                                            INSERT INTO Catalog.AttributePeaks (Id, AttributeId, Position, Height, StandardDeviation, Mean)
                                                            VALUES (@Id, @AttributeId, @Position, @Height, @StandardDeviation, @Mean)
                                                            """, mode);
                }
                finally
                {
                    _semaphore.Release();
                    _logger.LogWarning("Lock released from {Relation} {Attribute}", relationId, attributeEntity.Name);
                }
            }
        }

        return (relationMetrics, attributeMetrics);
    }
}