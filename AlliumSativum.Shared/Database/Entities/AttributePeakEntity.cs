namespace AlliumSativum.Shared.Database.Entities;

public sealed class AttributePeakEntity
{
    public Guid Id { get; set; }
    public Guid AttributeId { get; set; }
    public double Position { get; set; }
    public int Height { get; set; }
    public double StandardDeviation { get; set; }
    public double Mean { get; set; }
    
    
    /// <summary>
    /// Density, NOT PART OF THE SQL MODEl
    /// </summary>
    public double Density { get; set; }
}
