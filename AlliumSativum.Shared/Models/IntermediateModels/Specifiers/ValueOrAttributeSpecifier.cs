namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

public sealed class ValueOrAttributeSpecifier
{
    public string? Value { get; set; }
    public AttributeSpecifier? Attribute { get; set; }
}
