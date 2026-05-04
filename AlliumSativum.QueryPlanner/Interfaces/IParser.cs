using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Interfaces;

public interface ITokenQueryParser
{
    SelectBaseModel? Parse(Stack<string> tokens);
}
