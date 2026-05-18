using System.Dynamic;

namespace AlliumSativum.Shared.Utils;

public static class DictionaryHelper
{
    /// <summary>
    /// Prefixes the keys of a dictionary with a specified string.
    /// </summary>
    /// <param name="source">The dictionary to prefix</param>
    /// <param name="prefix">The prefix</param>
    /// <typeparam name="TValue"></typeparam>
    /// <returns>The prefixed dictionary</returns>
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