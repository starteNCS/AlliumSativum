namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

/// <summary>
/// Specifier containing the mapping between a variable and the attribute it represents.
/// </summary>
public sealed class VariableMappingSpecifier : ISpecifier
{
    public VariableMappingSpecifier(string variableName, string attributeName)
    {
        VariableName = variableName;
        AttributeName = attributeName;
    }

    public string VariableName { get; set; }
    public string AttributeName { get; set; }
}