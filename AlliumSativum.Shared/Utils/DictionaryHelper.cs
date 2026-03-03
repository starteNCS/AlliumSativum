using System.Dynamic;

namespace AlliumSativum.Shared.Utils;

public static class DictionaryHelper
{
    public static dynamic ToDynamic(this IDictionary<string, object> dictionary)
    {
        var expando = new ExpandoObject();
        var expandoDict = (IDictionary<string, object>)expando;

        foreach (var kvp in dictionary)
        {
            expandoDict[kvp.Key] = kvp.Value;
        }

        return expando;
    }
    
    public static Dictionary<string, TValue> PrefixKeys<TValue>(
        this Dictionary<string, TValue> source, 
        string prefix)
    {
        return source.ToDictionary(
            kvp => $"{prefix}{kvp.Key}", 
            kvp => kvp.Value
        );
    }
}
