using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Interfaces;

public interface ISemanticTransformer
{
    /// <summary>
    ///     Transforms in place
    /// </summary>
    /// <param name="model"></param>
    void Transform(SelectDto model);
}
