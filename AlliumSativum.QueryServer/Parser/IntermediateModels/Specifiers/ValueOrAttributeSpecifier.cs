namespace AlliumSativum.Parser.IntermediateModels.Specifiers;

public sealed class ValueOrAttributeSpecifier
{
    public string? Value { get; set; }
    public AttributeSpecifier? Attribute { get; set; }
}
