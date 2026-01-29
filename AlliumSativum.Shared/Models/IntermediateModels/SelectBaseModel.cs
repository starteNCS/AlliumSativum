using System.Text;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.IntermediateModels;

public sealed class SelectBaseModel
{
    public List<VariableMapping> VariableMappings { get; set; } = [];
    public List<ISpecifier> Select { get; set; } = new List<ISpecifier>();
    public TableSpecifier? From { get; set; }
    public IExpressionNode? Where { get; set; } 
    public List<JoinBaseModel> Join { get; set; } = new List<JoinBaseModel>();

    public string ToPostgreSqlString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("SELECT ");
        foreach (var specifier in Select)
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