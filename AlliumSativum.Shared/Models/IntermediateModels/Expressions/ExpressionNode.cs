using System.Text.Json.Serialization;
using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.IntermediateModels.Expressions;

// fix empty output for ASP.NET endpoints
[JsonPolymorphic(IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(PartialColumnExpressionNode), typeDiscriminator: "partial")]
[JsonDerivedType(typeof(VariableMappingExpressionNode), typeDiscriminator: "variable")]
[JsonDerivedType(typeof(FullySpecifiedColumnExpressionNode), typeDiscriminator: "fullySpecified")]
[JsonDerivedType(typeof(ValueExpressionNode), typeDiscriminator: "value")]
[JsonDerivedType(typeof(BinaryOperatorExpressionNode), typeDiscriminator: "binary")]
public abstract class ExpressionNode
{
    public abstract string ToSqlQueryString();
    public abstract bool Equals(object? obj);
    
    public bool IsPurelyTables(List<TableSpecifier> table)
    {
        return this switch
        {
            ValueExpressionNode => true,
            FullySpecifiedColumnExpressionNode fully => table.Exists(x => x.TableName == fully.Attribute.TableName &&  x.DataSourceName == fully.Attribute.DataSourceName),
            BinaryOperatorExpressionNode binary =>
                binary.Left.IsPurelyTables(table) && binary.Right.IsPurelyTables(table),
            _ => false
        };
    }
    
    public List<AttributeSpecifier> GetAttributesOfExpression()
    {
        var results = new HashSet<AttributeSpecifier>();
        var stack = new Stack<ExpressionNode>();
    
        stack.Push(this);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            switch (current)
            {
                case FullySpecifiedColumnExpressionNode fully:
                    results.Add(fully.Attribute);
                    break;

                case BinaryOperatorExpressionNode binary:
                    stack.Push(binary.Right);
                    stack.Push(binary.Left);
                    break;

                case VariableMappingExpressionNode varMap:
                    throw new ArgumentException($"Variable mapping is not expected at this point. Should have been expanded by the semantic transformer. Did not expect alias {varMap.VariableMapping.VariableName}");
            }
        }

        return results.ToList();
    }

    public List<TableSpecifier> GetTablesOfExpression()
    {
        return GetAttributesOfExpression()
            .Select(x => new TableSpecifier(x.DataSourceName, x.TableName))
            .Distinct()
            .ToList();
    }
}

public class PartialColumnExpressionNode : ExpressionNode
{
    public string Name { get; set; } = string.Empty;
    public override string ToString() => $"[{Name}]";
    public override string ToSqlQueryString() => "invalid";

    public override bool Equals(object? obj)
    {
        if (obj is not PartialColumnExpressionNode other)
        {
            return false;
        }
        
        return other.Name.Equals(Name);
    }
}

public class VariableMappingExpressionNode : ExpressionNode
{
    public required VariableMappingSpecifier VariableMapping { get; set; }
    public override string ToString() => $"[{VariableMapping.VariableName}{AsSqlParameters.Attribute.TableSeparator}{VariableMapping.AttributeName}]";
    public override string ToSqlQueryString() => $"{VariableMapping.VariableName}.{VariableMapping.AttributeName}";
    
    public override bool Equals(object? obj)
    {
        if (obj is not VariableMappingExpressionNode other)
        {
            return false;
        }
        
        return other.VariableMapping.Equals(VariableMapping);
    }
}

public class FullySpecifiedColumnExpressionNode : ExpressionNode
{
    public required AttributeSpecifier Attribute { get; set; }
    public override string ToString() => $"[{Attribute.DataSourceName}{AsSqlParameters.Attribute.DataSourceSeparator}{Attribute.TableName}{AsSqlParameters.Attribute.TableSeparator}{Attribute.AttributeName}]";
    
    // currently discards the data source attribute
    public override string ToSqlQueryString() => $"{Attribute.TableName}.{Attribute.AttributeName}";
    
    public override bool Equals(object? obj)
    {
        if (obj is not FullySpecifiedColumnExpressionNode other)
        {
            return false;
        }
        
        return other.Attribute.Equals(Attribute);
    }
}

public class ValueExpressionNode : ExpressionNode
{
    public ValueExpressionType Type { get; init; }
    public string Value { get; init; } = string.Empty;
    public override string ToString() => Type switch
    {
        ValueExpressionType.String => $"'{Value}'",
        ValueExpressionType.Decimal => Value,
        _ => "false"
    };
    public override string ToSqlQueryString() => ToString();
    
    public override bool Equals(object? obj)
    {
        if (obj is not ValueExpressionNode other)
        {
            return false;
        }
        
        return other.Value.Equals(Value) &&  other.Type.Equals(Type);
    }

    public enum ValueExpressionType
    {
        String = 0,
        Decimal = 1
    }
}

public class BinaryOperatorExpressionNode : ExpressionNode
{
    public string Operation { get; set; } = string.Empty;
    public required ExpressionNode Left { get; set; }
    public required ExpressionNode Right { get; set; }
    public override string ToString() => $"({Left} {Operation} {Right})";
    public override string ToSqlQueryString() => $"({Left.ToSqlQueryString()} {Operation} {Right.ToSqlQueryString()})";
    
    public override bool Equals(object? obj)
    {
        if (obj is not BinaryOperatorExpressionNode other)
        {
            return false;
        }
        
        return other.Operation == Operation && other.Left.Equals(Left) && other.Right.Equals(Right);
    }
}
