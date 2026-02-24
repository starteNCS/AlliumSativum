using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Models.IntermediateModels;

public interface IIntermediateJoinNode
{
}

public class IntermediateJoinTreeTableSpecifier : TableSpecifier, IIntermediateJoinNode
{
    public IntermediateJoinTreeTableSpecifier(string dataSourceName, string tableName) : base(dataSourceName, tableName)
    {
    }

    public static IntermediateJoinTreeTableSpecifier FromTableSpecifier(TableSpecifier tableSpecifier)
    {
        return new IntermediateJoinTreeTableSpecifier(tableSpecifier.DataSourceName, tableSpecifier.TableName);
    }

    public TableSpecifier ToTableSpecifier()
    {
        return new TableSpecifier(DataSourceName, TableName);
    }
}

public class IntermediateJoinNode : IIntermediateJoinNode
{
    public required IIntermediateJoinNode Left { get; set; }
    
    public required ExpressionNode Expression { get; init; }

    public required IIntermediateJoinNode Right { get; set; }
}