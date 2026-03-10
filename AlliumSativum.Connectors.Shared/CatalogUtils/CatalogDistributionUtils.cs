using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators.Models;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Connectors.Shared.CatalogUtils;

public sealed class CatalogDistributionUtils
{
    private readonly CatalogDatabase _catalog;

    public CatalogDistributionUtils(CatalogDatabase catalog)
    {
        _catalog = catalog;
    }
    
    public async Task<Dictionary<AttributeSpecifier, PlanOperatorDistributionData>> GetAttributeDistributionsAsync(List<AttributeSpecifier> attributes)
    {
        List<Task<List<AttributeEntity>>> tasks = [];
        foreach (var group in attributes.GroupBy(x => new {x.DataSourceName, x.TableName}))
        {
            tasks.Add(_catalog.QueryAsync<AttributeEntity>("""
                                                           SELECT a.* 
                                                           FROM catalog.attributes a 
                                                               JOIN catalog.relations r ON a.relationid = r.id 
                                                               JOIN catalog.datasources d ON r.datasourceid = d.id
                                                           WHERE d.name = @DataSourceName AND r.name LIKE @TableName AND a.name = ANY(@AttributeNames)
                                                           """, new
            {
                DataSourceName = group.Key.DataSourceName,
                TableName = $"%{group.Key.TableName}",
                AttributeNames = group.Select(a => a.AttributeName).ToArray()
            }));
        }

        var attributeEntityResolvedTasks = await Task.WhenAll(tasks);
        var attributeEntities = attributeEntityResolvedTasks.SelectMany(t => t).ToList();
        
        var peaks = await _catalog.QueryAsync<AttributePeakEntity>("SELECT ap.* FROM catalog.attributepeaks ap WHERE ap.attributeid = ANY(@AttributeIds)", new
        {
            AttributeIds = attributeEntities.Select(a => a.Id).ToArray()
        });
        
        var distributions = new Dictionary<AttributeSpecifier, PlanOperatorDistributionData>();
        foreach (var attribute in attributes)
        {
            var attributeEntity = attributeEntities.SingleOrDefault(a => a.Name == attribute.AttributeName);
            if (attributeEntity is null)
            {
                continue;
            }
            
            var attributePeaks = peaks.Where(p => p.AttributeId == attributeEntity.Id).ToList();
            distributions.Add(attribute, new PlanOperatorDistributionData
            {
                DistributionType =  attributeEntity.DistributionType,
                Min = attributeEntity.Min ?? double.NaN,
                Max = attributeEntity.Max ?? double.NaN,
                Peaks = attributePeaks.Select(ap => new PlanOperatorDistributionData.Peak
                {
                    Position = ap.Position,
                    Height = ap.Height,
                }).ToList(),
            });
        }
        
        return distributions;
    }
}
