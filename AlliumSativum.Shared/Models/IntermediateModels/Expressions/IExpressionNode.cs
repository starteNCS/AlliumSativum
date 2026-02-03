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
}

public class PartialColumnExpressionNode : IExpressionNode
{
    public string Name { get; set; } = string.Empty;
    public override string ToString() => $"[{Name}]";
    public string ToSqlQueryString() => "invalid";
}

public class VariableMappingExpressionNode : IExpressionNode
{
    public required VariableMappingSpecifier VariableMapping { get; set; }
    public override string ToString() => $"[{VariableMapping.VariableName}{AsSqlParameters.Attribute.TableSeparator}{VariableMapping.AttributeName}]";

    public string ToSqlQueryString() => $"{VariableMapping.VariableName}.{VariableMapping.AttributeName}";
}

public class FullySpecifiedColumnExpressionNode : IExpressionNode
{
    public required AttributeSpecifier Attribute { get; set; }
    public override string ToString() => $"[{Attribute.DataSourceName}{AsSqlParameters.Attribute.DataSourceSeparator}{Attribute.TableName}{AsSqlParameters.Attribute.TableSeparator}{Attribute.AttributeName}]";
    
    // currently discards the data source attribute
    public string ToSqlQueryString() => $"{Attribute.TableName}.{Attribute.AttributeName}";
}

public class ValueExpressionNode : IExpressionNode
{
    // TODO: add type annotation
    public string Value { get; set; } = string.Empty;
    public override string ToString() => $"'{Value}'";
    
    public string ToSqlQueryString() => ToString();
}

public class BinaryOperatorExpressionNode : IExpressionNode
{
    public string Operation { get; set; } = string.Empty;
    public required IExpressionNode Left { get; set; }
    public required IExpressionNode Right { get; set; }

    public override string ToString() => $"({Left} {Operation} {Right})";

    public string ToSqlQueryString() => $"({Left.ToSqlQueryString()} {Operation} {Right.ToSqlQueryString()})";
}
