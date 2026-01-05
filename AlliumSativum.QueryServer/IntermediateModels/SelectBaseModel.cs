namespace AlliumSativum.IntermediateModels;

public sealed class SelectBaseModel
{
    public required IList<AttributeSpecifier> Select { get; set; } = new List<AttributeSpecifier>();
    public required TableSpecifier From { get; set; }
    public required IList<Expression> Where { get; set; } = new List<Expression>();
    public required IList<JoinBaseModel> Join { get; set; } = new List<JoinBaseModel>();
}