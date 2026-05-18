using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Utils;

public static class ListExtensions
{
    /// <summary>
    /// Extension method to append attributes as hidden
    /// </summary>
    /// <param name="list">base list</param>
    /// <param name="hiddenAttributes">new hidden attributes</param>
    /// <returns>New list containing all hidden attribtues</returns>
    public static List<ISpecifier> AppendHiddenAttributes(this List<ISpecifier> list,
        List<AttributeSpecifier> hiddenAttributes)
    {
        foreach (var attribute in hiddenAttributes)
        {
            if (list.Any(a => a is AttributeSpecifier aSpec && aSpec.Equals(attribute))) continue;

            attribute.IsHidden = true;
            list.Add(attribute);
        }

        return list;
    }
}