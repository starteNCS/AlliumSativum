using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Interfaces;

public interface ITokenQueryParser
{
    SelectDto? Parse(Stack<string> tokens);
}
