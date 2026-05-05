using AlliumSativum.Interfaces;
using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Parser;

public partial class TokenQueryParser : ITokenQueryParser
{
    public SelectDto? Parse(Stack<string> tokens)
    {
        var select = new SelectDto();

        while (tokens.Count > 0) HandleTopStatement(tokens, select);


        return select;
    }

    private void HandleTopStatement(Stack<string> tokens, SelectDto model)
    {
        switch (tokens.Peek())
        {
            case AsSqlKeywords.SELECT:
                HandleSelectStatement(tokens, model);
                break;
            case AsSqlKeywords.FROM:
                HandleFromStatement(tokens, model);
                break;
            case AsSqlKeywords.WHERE:
                HandleWhereStatement(tokens, model);
                break;
            case AsSqlKeywords.JoinType.INNER:
                // case AsSqlKeywords.JoinType.OUTER:
                // case AsSqlKeywords.JoinType.LEFT:
                // case AsSqlKeywords.JoinType.RIGHT:
                HandleJoinStatement(tokens, model);
                break;
            default:
                tokens.Pop();
                break;
        }
    }
}