namespace AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

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