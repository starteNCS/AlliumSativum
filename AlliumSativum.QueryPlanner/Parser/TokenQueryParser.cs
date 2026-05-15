using AlliumSativum.Interfaces;
using AlliumSativum.Shared.Constants;
using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Parser;

public partial class TokenQueryParser : ITokenQueryParser
{
    /// <inheritdoc/>
    public SelectDto? Parse(Stack<string> tokens)
    {
        var select = new SelectDto();

        while (tokens.Count > 0) HandleTopStatement(tokens, select);


        return select;
    }

    /// <summary>
    /// Given a stack of tokens, determines which part of the query should be parsed next and calls the appropriate handler method
    /// </summary>
    /// <param name="tokens">Stack of tokens</param>
    /// <param name="model">Current resulting model, with all properties set according to previous statements</param>
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