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
public interface IExpressionNode
{
    public string ToSqlQueryString();
    public bool Equals(object? obj);
}

public class PartialColumnExpressionNode : IExpressionNode
{
    public string Name { get; set; } = string.Empty;
    public override string ToString() => $"[{Name}]";
    public string ToSqlQueryString() => "invalid";

    public override bool Equals(object? obj)
    {
        if (obj is not PartialColumnExpressionNode other)
        {
            return false;
        }
        
        return other.Name.Equals(Name);
    }
}

public class VariableMappingExpressionNode : IExpressionNode
{
    public required VariableMappingSpecifier VariableMapping { get; set; }
    public override string ToString() => $"[{VariableMapping.VariableName}{AsSqlParameters.Attribute.TableSeparator}{VariableMapping.AttributeName}]";
    public string ToSqlQueryString() => $"{VariableMapping.VariableName}.{VariableMapping.AttributeName}";
    
    public override bool Equals(object? obj)
    {
        if (obj is not VariableMappingExpressionNode other)
        {
            return false;
        }
        
        return other.VariableMapping.Equals(VariableMapping);
    }
}

public class FullySpecifiedColumnExpressionNode : IExpressionNode
{
    public required AttributeSpecifier Attribute { get; set; }
    public override string ToString() => $"[{Attribute.DataSourceName}{AsSqlParameters.Attribute.DataSourceSeparator}{Attribute.TableName}{AsSqlParameters.Attribute.TableSeparator}{Attribute.AttributeName}]";
    
    // currently discards the data source attribute
    public string ToSqlQueryString() => $"{Attribute.TableName}.{Attribute.AttributeName}";
    
    public override bool Equals(object? obj)
    {
        if (obj is not FullySpecifiedColumnExpressionNode other)
        {
            return false;
        }
        
        return other.Attribute.Equals(Attribute);
    }
}

public class ValueExpressionNode : IExpressionNode
{
    public ValueExpressionType Type { get; init; }
    public string Value { get; init; } = string.Empty;
    public override string ToString() => Type switch
    {
        ValueExpressionType.String => $"'{Value}'",
        ValueExpressionType.Decimal => Value,
        _ => "false"
    };
    public string ToSqlQueryString() => ToString();
    
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

public class BinaryOperatorExpressionNode : IExpressionNode
{
    public string Operation { get; set; } = string.Empty;
    public required IExpressionNode Left { get; set; }
    public required IExpressionNode Right { get; set; }
    public override string ToString() => $"({Left} {Operation} {Right})";
    public string ToSqlQueryString() => $"({Left.ToSqlQueryString()} {Operation} {Right.ToSqlQueryString()})";
    
    public override bool Equals(object? obj)
    {
        if (obj is not BinaryOperatorExpressionNode other)
        {
            return false;
        }
        
        return other.Operation == Operation && other.Left.Equals(Left) && other.Right.Equals(Right);
    }
}
