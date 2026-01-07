using AlliumSativum.Parser.IntermediateModels.Expressions;
using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser.IntermediateModels;

public sealed class SelectBaseModel
{
    public IList<VariableMapping> VariableMappings { get; set; } = [];
    public IList<AttributeSpecifier> Select { get; set; } = new List<AttributeSpecifier>();
    public TableSpecifier? From { get; set; }
    public IExpressionNode? Where { get; set; } 
    public IList<JoinBaseModel> Join { get; set; } = new List<JoinBaseModel>();
}