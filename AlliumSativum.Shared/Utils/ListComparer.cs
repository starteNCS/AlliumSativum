namespace AlliumSativum.Shared.Utils;

public class ListComparer<T> : IEqualityComparer<List<T>>
{
    public bool Equals(List<T>? x, List<T>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null) return false;
        return x.SequenceEqual(y);
    }

    public int GetHashCode(List<T> obj)
    {
        var hash = new HashCode();
        foreach (var item in obj) hash.Add(item);
        return hash.ToHashCode();
    }
}