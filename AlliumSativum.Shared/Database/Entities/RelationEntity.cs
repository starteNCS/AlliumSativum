using AlliumSativum.Shared.Enums;

namespace AlliumSativum.Shared.Database.Entities;

public sealed class RelationEntity
{
    public Guid Id { get; set; }
    public Guid DataSourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Cardinality { get; set; }
    public long ConnectionOpenMs { get; set; }
    public long Transfer100Ms { get; set; }
    public DateTime MetricsDate { get; set; }
    public string AccessPath { get; set; } = string.Empty;
}
