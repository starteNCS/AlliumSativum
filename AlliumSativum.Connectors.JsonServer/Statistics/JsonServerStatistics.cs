using System.Text.Json;
using AlliumSativum.Connectors.Shared;
using AlliumSativum.Connectors.Shared.HttpUtils;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Utils;
using AlliumSavitum.Connectors.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace AlliumSativum.Connectors.JsonServer.Statistics;

public sealed class JsonServerStatistics : IDataSourceStatistics
{
    private readonly CatalogDatabase _catalog;

    public JsonServerStatistics(
        CatalogDatabase catalog)
    {
        _catalog = catalog;
    }

    public async Task ScrapeStatistics(Guid dataSource)
    {
        var dataSourceEntity = await _catalog.GetDataSourceAsync(dataSource);
        if (dataSourceEntity is null) return;
        var relations = await _catalog.GetRelationsOfDataSourceAsync(dataSourceEntity.Id);
        var attributes = await _catalog.GetAttributesOfDataSourceAsync(dataSourceEntity.Id);

        await _catalog.BeginTransactionAsync();
        foreach (var relation in relations)
        {
            var url = $"{dataSourceEntity.ConnectionString}/{relation.AccessPath}";
            var connectionMetrics = await HttpMetricsScraper.MeasureRequestAsync<dynamic>($"{url}?per_page=100");
            var total = await HttpMetricsScraper.MeasureRequestAsync<List<Dictionary<string, JsonElement>>>(url);

            relation.ConnectionOpenMs = connectionMetrics.ConnectionOpenTotal;
            relation.Transfer100Ms = connectionMetrics.TotalElapsed;
            if (total.Response is null)
                throw new AsSqlException(
                    $"Failed to retrieve data for relation {relation.Name} when calculating statistics.");
            relation.Cardinality = total.Response.Count;
            relation.MetricsDate = DateTime.Now;

            var relationAttributes = attributes.Where(a => a.RelationId == relation.Id);
            foreach (var relationAttribute in relationAttributes)
            {
                relationAttribute.DistinctCardinality = total.Response
                    .Select(x =>
                    {
                        if (!x.TryGetValue(relationAttribute.Name, out var value)) return null;
                        return value.ToString();
                    })
                    .Distinct()
                    .Count();
                relationAttribute.MetricsDate = DateTime.Now;
                var nummeric = total.Response
                    .Select<Dictionary<string, JsonElement>, double?>(x =>
                    {
                        if (!x.TryGetValue(relationAttribute.Name, out var jsonElement)) return null;

                        if (jsonElement.ValueKind == JsonValueKind.Number) return jsonElement.GetDouble();

                        return null;
                    })
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .ToList();
                relationAttribute.Min = nummeric.Count != 0 ? nummeric.Min() : null;
                relationAttribute.Max = nummeric.Count != 0 ? nummeric.Max() : null;
                if (total.Response[0].TryGetValue(relationAttribute.Name, out var value))
                    relationAttribute.DataType = value.ValueKind.ToString();

                var values = total.Response
                    .Select(x =>
                    {
                        if (!x.TryGetValue(relationAttribute.Name, out var jsonElement)) throw new AsSqlException("");

                        return jsonElement;
                    }).ToList();
                var (relationAttributeWithDistribution, modes) = CalculateDistribution(values, relationAttribute);

                await _catalog.ExecuteAsync("""
                                            UPDATE Catalog.Attributes
                                            SET DistinctCardinality = @DistinctCardinality,
                                                MetricsDate = @MetricsDate,
                                                Min = @Min,
                                                Max = @Max,
                                                Mean = @Mean,
                                                MeanBinHeight = @MeanBinHeight,
                                                Variance = @Variance,
                                                StandardDeviation = @StandardDeviation,
                                                Range = @Range,
                                                Skewness = @Skewness,
                                                Kurtosis = @Kurtosis,
                                                DataType = @DataType,
                                                DistributionType = @DistributionType
                                                WHERE Id = @Id
                                            """, relationAttributeWithDistribution);

                await _catalog.ExecuteAsync("DELETE FROM Catalog.AttributePeaks WHERE AttributeId = @AttributeId",
                    new { AttributeId = relationAttribute.Id });
                foreach (var mode in modes)
                    await _catalog.ExecuteAsync("""
                                                INSERT INTO Catalog.AttributePeaks (Id, AttributeId, Position, Height, StandardDeviation, Mean)
                                                VALUES (@Id, @AttributeId, @Position, @Height, @StandardDeviation, @Mean)
                                                """, mode);
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

    private static (AttributeEntity attribute, List<AttributePeakEntity> modes) CalculateDistribution(
        List<JsonElement> values, AttributeEntity attribute)
    {
        var type = values.FirstOrDefault(x => x.ValueKind != JsonValueKind.Null).ValueKind;
        switch (type)
        {
            case JsonValueKind.Number:
            {
                var parsedValues = values.Select(double? (x) =>
                {
                    if (x.ValueKind == JsonValueKind.Null) return null;

                    return x.TryGetDouble(out var num) ? num : null;
                }).ToList();
                return DistributionUtils.CalculateDistribution(parsedValues, attribute);
            }
            case JsonValueKind.String:
            {
                var parsedValues = values.Select(x => x.GetString() ?? string.Empty).ToList();
                return DistributionUtils.CalculateDistribution(parsedValues, attribute);
            }
            default:
                throw new AsSqlException(
                    "Only numeric and string attributes are supported for distribution calculation.");
        }
    }
}