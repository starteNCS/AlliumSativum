using AlliumSativum.Parser.Constants;
using AlliumSativum.Parser.IntermediateModels.Specifiers;

namespace AlliumSativum.Parser.IntermediateModels.Expressions;

public interface IWhereNode { }

public class PartialColumnWhereNode : IWhereNode
{
    public string Name { get; set; } = string.Empty;
    public override string ToString() => $"[{Name}]";
}

public class FullySpecifiedColumnWhereNode : IWhereNode
{
    public required AttributeSpecifier Attribute { get; set; }
    public override string ToString() => $"[{Attribute.DataSourceName}{AsSqlParameters.Attribute.DataSourceSeparator}{Attribute.TableName}{AsSqlParameters.Attribute.TableSeparator}{Attribute.AttributeName}]";
}

public class ValueWhereNode : IWhereNode
{
    public string Value { get; set; } = string.Empty;
    public override string ToString() => $"'{Value}'";
}

public class BinaryOperatorWhereNode : IWhereNode
{
    public string Operation { get; set; } = string.Empty;
    public required IWhereNode Left { get; set; }
    public required IWhereNode Right { get; set; }

    public override string ToString() => $"({Left} {Operation} {Right})";
}
