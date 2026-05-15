using AlliumSativum.Shared.Models.IntermediateModels;

namespace AlliumSativum.Interfaces;

public interface ISemanticTransformer
{
    /// <summary>
    /// Transforms the dto by expanding all variable mappings (that is, replacing all variables with their mapped values)
    /// </summary>
    /// <remarks>
    /// Transforms the given SelectDto in place
    /// </remarks>
    /// <param name="model">Current select dto</param>
    void Transform(SelectDto model);
}
