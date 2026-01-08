using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser.IntermediateModels;

public sealed class SelectBaseModel
{
    public List<VariableMapping> VariableMappings { get; set; } = [];
    public List<ISpecifier> Select { get; set; } = new List<ISpecifier>();
    public TableSpecifier? From { get; set; }
    public IExpressionNode? Where { get; set; } 
    public List<JoinBaseModel> Join { get; set; } = new List<JoinBaseModel>();
}