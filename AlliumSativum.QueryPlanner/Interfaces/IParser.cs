using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Interfaces;

public interface ITokenQueryParser
{
    /// <summary>
    /// Parses the token stack into a SelectDTO.
    /// Dto is a 1:1 representation of the query, that can be used to generate the query execution plan
    /// </summary>
    /// <param name="tokens">Parsed AsSQL Query Tokens</param>
    /// <returns>Object representing the query</returns>
    SelectDto? Parse(Stack<string> tokens);
}
