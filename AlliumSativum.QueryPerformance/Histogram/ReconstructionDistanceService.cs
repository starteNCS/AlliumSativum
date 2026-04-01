using AlliumSativum.Compiler;
using AlliumSativum.QueryServer.Utils;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Utils;
using Microsoft.Extensions.Logging;

namespace AlliumSativum.QueryExecutor.Performance.Histogram;

public sealed class ReconstructionDistanceService
{
    private readonly CatalogDatabase _catalog;
    private readonly QueryCompiler _compiler;
    private readonly ICostModel _costModel;
    private readonly DataUtils _dataUtils;
    private readonly ILogger<ReconstructionDistanceService> _logger;

    public ReconstructionDistanceService(
        QueryCompiler compiler,
        DataUtils dataUtils,
        ICostModel costModel,
        CatalogDatabase catalog,
        ILogger<ReconstructionDistanceService> logger)
    {
        _compiler = compiler;
        _dataUtils = dataUtils;
        _costModel = costModel;
        _catalog = catalog;
        _logger = logger;
    }

    public async Task<ReconstructionSimilarityResult> ReconstructionSimilarityOfDatasourceAsync(Guid datasourceId,
        List<string> ignoreAttributes)
    {
        var attributes = await _catalog.QueryAsync("""
                                                   SELECT a.name as AttributeName, r.name as RelationName, d.Name as DataSourceName
                                                   FROM catalog.attributes a 
                                                   INNER JOIN catalog.relations r on a.relationid = r.id
                                                   INNER JOIN catalog.datasources d on r.datasourceid = d.id
                                                   WHERE d.id = @DataSourceId AND a.datatype = ANY(ARRAY['smallint', 'integer', 'bigint', 'decimal', 'numeric', 'real', 'double precision', 'Number'])
                                                   """, new
        {
            DataSourceId = datasourceId
        });

        var queries = new List<string>();
        foreach (var attribute in attributes)
        {
            if (ignoreAttributes.Contains(attribute["attributename"])) continue;

            var relationSplits = ((string)attribute["relationname"]).Split('.');
            var relationName = relationSplits.Length > 1 ? relationSplits.Last() : (string)attribute["relationname"];
            
            var query =
                $"SELECT x.{attribute["attributename"]} FROM {attribute["datasourcename"]}->{relationName} x";
            queries.Add(query);
        }

        return await ReconstructionSimilarityAsync(queries);
    }
    
    public async Task<ReconstructionSimilarityResult> ReconstructionSimilarityOfAllDatasourcesAsync(List<string> ignoreAttributes)
    {
        var attributes = await _catalog.QueryAsync("""
                                                   SELECT a.name as AttributeName, r.name as RelationName, d.Name as DataSourceName
                                                   FROM catalog.attributes a 
                                                   INNER JOIN catalog.relations r on a.relationid = r.id
                                                   INNER JOIN catalog.datasources d on r.datasourceid = d.id
                                                   WHERE a.datatype = ANY(ARRAY['smallint', 'integer', 'bigint', 'decimal', 'numeric', 'real', 'double precision', 'Number'])
                                                   """);

        var queries = new List<string>();
        foreach (var attribute in attributes)
        {
            if (ignoreAttributes.Contains(attribute["attributename"])) continue;

            var relationSplits = ((string)attribute["relationname"]).Split('.');
            var relationName = relationSplits.Length > 1 ? relationSplits.Last() : (string)attribute["relationname"];
            
            var query =
                $"SELECT x.{attribute["attributename"]} FROM {attribute["datasourcename"]}->{relationName} x";
            queries.Add(query);
        }

        return await ReconstructionSimilarityAsync(queries);
    }

    public async Task<ReconstructionSimilarityResult> ReconstructionSimilarityAsync(List<string> queries)
    {
        var results = new List<EarthMoversDistanceResult>();

        foreach (var query in queries)
        {
            var plan = await _compiler.CompileAsync(query);

            var parsed = await _dataUtils.LoadDataAsync(plan);
            var histogram = parsed
                .GroupBy(x => x)
                .OrderBy(x => x.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            var (attribute, modes) =
                DistributionUtils.CalculateDistribution(parsed.Select(x => (double?)x).ToList(), new AttributeEntity());
            var reconstruction = _costModel.ReconstructDistribution(new PlanOperatorDistributionData
            {
                Min = attribute.Min ?? 0,
                Max = attribute.Max ?? 0,
                Mean = attribute.Mean,
                MeanBinHeight = attribute.MeanBinHeight,
                DistributionType = attribute.DistributionType,
                Peaks = modes.Select(peak => new PlanOperatorDistributionData.Peak
                {
                    Mean = peak.Mean,
                    Height = peak.Height,
                    Position = peak.Position,
                    StandardDeviation = peak.StandardDeviation
                }).ToList()
            });

            results.Add(new EarthMoversDistanceResult
            {
                Query = query,
                Distance = CalculateNormalizedEmd(histogram, reconstruction)
            });
            _logger.LogInformation("Calculated EMD for query {Index} of {TotalCount}", results.Count, queries.Count);
        }

        return new ReconstructionSimilarityResult
        {
            AverageEmd = results.Average(x => x.Distance),
            Results = results
        };
    }

    private static double CalculateNormalizedEmd(Dictionary<double, int> original,
        Dictionary<double, double> reconstructed)
    {
        if (original.Count == 0 || reconstructed.Count == 0) return 1.0;

        var allKeys = original.Keys.Union(reconstructed.Keys).ToList();
        var min = allKeys.Min();
        var max = allKeys.Max();
        var range = max - min;

        // edge case where all values are the same
        if (Math.Abs(range) <= 0e-12)
            return Math.Abs(original.Keys.First() - reconstructed.Keys.FirstOrDefault()) < 0e-12 ? 0 : 1;

        var totalOriginal = original.Values.Sum();
        var totalReconstructed = reconstructed.Values.Sum();

        var sortedKeys = allKeys.OrderBy(x => x).ToList();

        var cumulativeOrig = 0.0;
        var cumulativeRec = 0.0;
        var normalizedEmd = 0.0;

        for (var i = 0; i < sortedKeys.Count - 1; i++)
        {
            var currentRawKey = sortedKeys[i];
            var nextRawKey = sortedKeys[i + 1];

            cumulativeOrig += (double)original.GetValueOrDefault(currentRawKey, 0) / totalOriginal;
            cumulativeRec += reconstructed.GetValueOrDefault(currentRawKey, 0) / totalReconstructed;

            var normalizedWidth = (nextRawKey - currentRawKey) / range;
            normalizedEmd += Math.Abs(cumulativeOrig - cumulativeRec) * normalizedWidth;
        }

        return normalizedEmd;
    }
}

public class ReconstructionSimilarityResult
{
    public double AverageEmd { get; set; }
    public List<EarthMoversDistanceResult> Results { get; set; } = [];
}

public class EarthMoversDistanceResult
{
    public double Distance { get; set; }
    public string Query { get; set; } = string.Empty;
}