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
    public double Mean { get; set; }
    public double Variance { get; set; }
    public double StandardDeviation { get; set; }
    public double Range { get; set; }
    public double Skewness { get; set; }
    public double Kurtosis { get; set; } 
    public string DataType { get; set; } = string.Empty;
    
    public bool IsNumeric => DataType is "smallint" or "integer" or "bigint" or "decimal" or "numeric" or "real" or "double precision";
}
