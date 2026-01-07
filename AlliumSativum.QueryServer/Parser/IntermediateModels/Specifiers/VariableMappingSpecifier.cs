namespace AlliumSativum.Parser.IntermediateModels.Specifiers;

public sealed class VariableMappingSpecifier : ISpecifier
{
    public string VariableName { get; set; }
    public string AttributeName { get; set; }

    public VariableMappingSpecifier(string variableName, string attributeName)
    {
        VariableName = variableName;
        AttributeName = attributeName;
    }
}
