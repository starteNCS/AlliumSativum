using AlliumSativum.Interfaces;
using AlliumSativum.Shared.Exceptions;
using AlliumSativum.Shared.Models.IntermediateModels;
using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Semantic;

public class SemanticTransformer : ISemanticTransformer
{
    /// <inheritdoc />
    public void Transform(SelectDto model)
    {
        model.Where = CollapseVariableMappingsOfExpression(model.VariableMappings, model.Where);

        foreach (var join in model.Join)
            join.Expression = CollapseVariableMappingsOfExpression(model.VariableMappings, join.Expression)!;

        model.Select = CollapseVariableMappingsOfSpecifiers(model.VariableMappings, model.Select);
    }

    /// <summary>
    /// Collapses variable mappings of the given specifiers by replacing all VariableMappingSpecifiers
    /// with AttributeSpecifiers based on the given variable mappings.
    /// </summary>
    /// <param name="variableMappings">All variable mappings of a query</param>
    /// <param name="specifiers">All projections of a query</param>
    /// <returns>The fully expanded list of attribute specifiers</returns>
    /// <exception cref="AsSqlSemanticException">A attribute specifier uses a non-existing variable mapping</exception>
    private static List<ISpecifier> CollapseVariableMappingsOfSpecifiers(List<VariableMapping> variableMappings,
        List<ISpecifier> specifiers)
    {
        for (var index = 0; index < specifiers.Count; index++)
        {
            if (specifiers[index] is not VariableMappingSpecifier mappingSpecifier) continue;

            var mapping = variableMappings.Find(m => m.Alias == mappingSpecifier.VariableName);
            if (mapping == null)
                throw new AsSqlSemanticException($"variable mapping not found: '{mappingSpecifier.VariableName}'");

            specifiers[index] = new AttributeSpecifier(
                mapping.Table.DataSourceName,
                mapping.Table.TableName,
                mappingSpecifier.AttributeName);
        }

        return specifiers;
    }

    /// <summary>
    /// Collapses variable mappings of the given expression by replacing all VariableMappingExpressionNodes
    /// with FullySpecifiedColumnExpressionNodes based on the given variable mappings.
    /// </summary>
    /// <param name="variableMappings">All variable mappings of a query</param>
    /// <param name="expression">The root node of an expression</param>
    /// <returns>The fully expanded expression node</returns>
    /// <exception cref="AsSqlSemanticException">A node uses a mapping that does not exist</exception>
    private ExpressionNode? CollapseVariableMappingsOfExpression(List<VariableMapping> variableMappings,
        ExpressionNode? expression)
    {
        if (expression == null) return null;

        var moveExpression = expression;
        CollapseVariableMappingsOfExpressionRef(variableMappings, ref moveExpression);
        if (moveExpression == null)
            throw new AsSqlSemanticException("Expected semantic checked expression to not be null, but was null");

        return moveExpression;
    }

    /// <summary>
    /// Hands over the expression as a ref parameter and collapses all variable mappings of the given expression by replacing all VariableMappingExpressionNodes
    /// with FullySpecifiedColumnExpressionNodes based on the given variable mappings.
    /// </summary>
    /// <param name="variableMappings">All variable mappings of a query</param>
    /// <param name="expression">The root node of an expression</param>
    /// <exception cref="AsSqlSemanticException">A node ues a mapping that does not exist</exception>
    private void CollapseVariableMappingsOfExpressionRef(List<VariableMapping> variableMappings,
        ref ExpressionNode? expression)
    {
        if (expression == null) return;

        if (expression is VariableMappingExpressionNode variableMapping)
        {
            var foundMapping = variableMappings.Find(m => m.Alias == variableMapping.VariableMapping.VariableName);
            if (foundMapping == null)
                throw new AsSqlSemanticException(
                    $"variable mapping not found: '{variableMapping.VariableMapping.VariableName}'");

            expression = new FullySpecifiedColumnExpressionNode
            {
                Attribute = new AttributeSpecifier(foundMapping.Table.DataSourceName, foundMapping.Table.TableName,
                    variableMapping.VariableMapping.AttributeName)
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