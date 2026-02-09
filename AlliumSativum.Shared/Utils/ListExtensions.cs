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
}
