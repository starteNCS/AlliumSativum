using System.Linq.Expressions;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Semantic;

/// <summary>
/// Semanatic analyzer
/// </summary>
public class SemanticTransformer
{
    /// <summary>
    /// Transforms in place
    /// </summary>
    /// <param name="model"></param>
    public void Transform(SelectBaseModel model)
    {
        model.Where = CollapseVariableMappingsOfExpression(model.VariableMappings, model.Where);
        
        foreach (var join in model.Join)
        {
            join.Expression = CollapseVariableMappingsOfExpression(model.VariableMappings, join.Expression)!;
        }
        
        model.Select = CollapseVariableMappingsOfSpecifiers(model.VariableMappings, model.Select);
    }

    private List<ISpecifier> CollapseVariableMappingsOfSpecifiers(List<VariableMapping> variableMappings, List<ISpecifier> specifiers)
    {
        for (int index = 0; index < specifiers.Count; index++)
        {
            if (specifiers[index] is not VariableMappingSpecifier mappingSpecifier)
            {
                continue;
            }
            
            var mapping = variableMappings.Find(m => m.Alias == mappingSpecifier.VariableName);
            if (mapping == null)
            {
                throw new AsSqlSemanticException($"variable mapping not found: '{mappingSpecifier.VariableName}'");
            }

            specifiers[index] = new AttributeSpecifier(
                mapping.Table.DataSourceName,
                mapping.Table.TableName,
                mappingSpecifier.AttributeName);
        }
        
        return specifiers;
    }

    private ExpressionNode? CollapseVariableMappingsOfExpression(List<VariableMapping> variableMappings, ExpressionNode? expression)
    {
        if (expression == null)
        {
            return null;
        }
        
        var moveExpression = expression;
        CollapseVariableMappingsOfExpressionRef(variableMappings, ref moveExpression);
        if (moveExpression == null)
        {
            throw new AsSqlSemanticException("Expected semantic checked expression to not be null, but was null");
        }
        
        return moveExpression;
    }

    private void CollapseVariableMappingsOfExpressionRef(List<VariableMapping> variableMappings, ref ExpressionNode? expression)
    {
        if (expression == null)
        {
            return;
        }

        if (expression is VariableMappingExpressionNode variableMapping)
        {
            var foundMapping = variableMappings.Find(m => m.Alias == variableMapping.VariableMapping.VariableName);
            if (foundMapping == null)
            {
                throw new AsSqlSemanticException($"variable mapping not found: '{variableMapping.VariableMapping.VariableName}'");
            }

            expression = new FullySpecifiedColumnExpressionNode()
            {
                Attribute = new AttributeSpecifier(foundMapping.Table.DataSourceName, foundMapping.Table.TableName, variableMapping.VariableMapping.AttributeName)
            };
            return;
        }
        
        if (expression is BinaryOperatorExpressionNode binaryExpression)
        {
            var left = binaryExpression.Left;
            var right = binaryExpression.Right;
            CollapseVariableMappingsOfExpressionRef(variableMappings, ref left);
            CollapseVariableMappingsOfExpressionRef(variableMappings, ref right);
            binaryExpression.Left = left;
            binaryExpression.Right = right;
        }
    }
}
