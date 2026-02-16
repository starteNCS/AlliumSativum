using AlliumSativum.Shared.Enums;

namespace AlliumSativum.Shared.Database.Entities;

public sealed class AttributeEntity
{
    public Guid Id { get; set; }
    public Guid RelationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long DistinctCardinality { get; set; }
    public DateTime MetricsDate { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public string DataType { get; set; } = string.Empty;
    
    public bool IsNummeric => DataType is "smallint" or "integer" or "bigint" or "decimal" or "numeric" or "real" or "double precision";
}
