using System.Diagnostics;
using AlliumSativum.Connectors.Shared.HttpUtils;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Exceptions;
using AlliumSavitum.Connectors.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace AlliumSativum.Connectors.TicketSystem.Statistics;

public sealed class TicketSystemStatistics : IDataSourceStatistics
{
    private readonly CatalogDatabase _catalog;
    private readonly ILogger<TicketSystemStatistics> _logger;

    public TicketSystemStatistics(
        CatalogDatabase catalog,
        ILogger<TicketSystemStatistics> logger)
    {
        _catalog = catalog;
        _logger = logger;
    }
    
    public async Task ScrapeStatistics(Guid dataSource)
    {
        var dataSourceEntity = await _catalog.GetDataSourceAsync(dataSource);
        if (dataSourceEntity is null)
        {
            return;
        }
        var relations = await _catalog.GetRelationsOfDataSourceAsync(dataSourceEntity.Id);
        var attributes = await _catalog.GetAttributesOfDataSourceAsync(dataSourceEntity.Id);

        await _catalog.BeginTransactionAsync();
        foreach (var relation in relations)
        {
            var url = $"{dataSourceEntity.ConnectionString}/{relation.AccessPath}";
            var connectionMetrics = await HttpMetricsScraper.MeasureRequestAsync<dynamic>($"{url}?per_page=100");
            var total = await HttpMetricsScraper.MeasureRequestAsync<List<Dictionary<string, object>>>(url);

            relation.ConnectionOpenMs = connectionMetrics.ConnectionOpenTotal;
            relation.Transfer100Ms = connectionMetrics.TotalElapsed;
            if (total.Response is null)
            {
                throw new AsSqlException();
            }
            relation.Cardinality = total.Response.Count;
            relation.MetricsDate = DateTime.Now;
            
            var relationAttributes = attributes.Where(a => a.RelationId == relation.Id);
            foreach (var relationAttribute in relationAttributes)
            {
                relationAttribute.DistinctCardinality = total.Response
                    .Select(x =>
                    {
                        x.TryGetValue(relation.Name, out var value);
                        return value;
                    })
                    .Distinct()
                    .Count();
                relationAttribute.MetricsDate = DateTime.Now;
                
                await _catalog.ExecuteAsync("""
                                            UPDATE Catalog.Attributes
                                            SET DistinctCardinality = @DistinctCardinality,
                                                MetricsDate = @MetricsDate
                                                WHERE Id = @Id
                                            """, relationAttribute);
            }
            
            await _catalog.ExecuteAsync("""
                                        UPDATE Catalog.Relations
                                        SET ConnectionOpenMs = @ConnectionOpenMs,
                                            Transfer100Ms = @Transfer100Ms,
                                            Cardinality = @Cardinality,
                                            MetricsDate = @MetricsDate
                                        WHERE Id = @Id
                                        """, relation);
        }

        await _catalog.CommitTransactionAsync();
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
