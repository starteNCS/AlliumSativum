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
    
    public static List<List<T>> GetPermutations<T>(this List<T> items)
    {
        if (items.Count > 5)
        {
            throw new InvalidOperationException("Generating permutations for more than 5 items is not supported due to performance reasons.");
        }
        
        if (items.Count == 0)
            return [[]];

        var results = new List<List<T>>();

        for (int i = 0; i < items.Count; i++)
        {
            var current = items[i];
            var remaining = items.Where((_, idx) => idx != i).ToList();

            foreach (var permutation in remaining.GetPermutations())
            {
                results.Add([current, ..permutation]);
            }
        }

        return results;
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
