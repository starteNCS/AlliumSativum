using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;

namespace AlliumSativum.Shared.Utils;

public static class ListExtensions
{
    public static T GetFirstAndRemove<T>(this List<T> list)
    {
        var item = list[0];
        list.Remove(item);
        return item;
    }
    
    public static T? GetAndRemove<T>(this List<T> list, Func<T, bool> predicate)
    {
        var item = list.FirstOrDefault(predicate);
        if (item is null)
        {
            return default;
        }
        
        list.Remove(item);
        return item;
    }
    
    public static List<ISpecifier> AppendHiddenAttributes(this List<ISpecifier> list, List<AttributeSpecifier> hiddenAttributes)
    {
        foreach (var attribute in hiddenAttributes)
        {
            if (list.Any(a => a is AttributeSpecifier aSpec && aSpec.Equals(attribute)))
            {
                continue;
            }
            
            attribute.IsHidden = true;
            list.Add(attribute);
        }

        return list;
    }
}
