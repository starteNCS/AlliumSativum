using System.Text;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.IntermediateModels;

public sealed class SelectBaseModel
{
    public List<VariableMapping> VariableMappings { get; set; } = [];
    public List<ISpecifier> Select { get; set; } = [];
    public TableSpecifier? From { get; set; }
    public IExpressionNode? Where { get; set; } 
    public List<JoinBaseModel> Join { get; set; } = [];

    public string ToPostgreSqlString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("SELECT ");
        var first = (AttributeSpecifier)Select[0];
        stringBuilder.Append($"{first.TableName}.{first.AttributeName}");
        foreach (var specifier in Select.Skip(1))
        {
            if (specifier is not AttributeSpecifier select)
            {
                continue;
            }
            
            stringBuilder.Append($", {select.TableName}.{select.AttributeName}");
        }

        stringBuilder.Append($"FROM {From?.TableName}");
        return stringBuilder.ToString();
    }
}