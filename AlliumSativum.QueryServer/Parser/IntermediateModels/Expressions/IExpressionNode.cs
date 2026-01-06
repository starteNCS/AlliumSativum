using AlliumSativum.Parser.Constants;
using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser.IntermediateModels.Expressions;

public interface IExpressionNode { }

public class PartialColumnExpressionNode : IExpressionNode
{
    public string Name { get; set; } = string.Empty;
    public override string ToString() => $"[{Name}]";
}

public class FullySpecifiedColumnExpressionNode : IExpressionNode
{
    public required AttributeSpecifier Attribute { get; set; }
    public override string ToString() => $"[{Attribute.DataSourceName}{AsSqlParameters.Attribute.DataSourceSeparator}{Attribute.TableName}{AsSqlParameters.Attribute.TableSeparator}{Attribute.AttributeName}]";
}

public class ValueExpressionNode : IExpressionNode
{
    public string Value { get; set; } = string.Empty;
    public override string ToString() => $"'{Value}'";
}

public class BinaryOperatorExpressionNode : IExpressionNode
{
    public string Operation { get; set; } = string.Empty;
    public required IExpressionNode Left { get; set; }
    public required IExpressionNode Right { get; set; }

    public override string ToString() => $"({Left} {Operation} {Right})";
}
