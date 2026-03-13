using BenchmarkDotNet.Attributes;

namespace AlliumSativum.Performance.Utils;

[MemoryDiagnoser]
[CsvExporter]
[CsvMeasurementsExporter]
public class HashTableLookupPerformanceTest
{
    private Dictionary<string, object> _hashTable;
    
    [Params(1, 10, 100, 1_000)]
    public double N;

    [GlobalSetup]
    public void Setup()
    {
        _hashTable = new Dictionary<string, object>();

        for (int i = 0; i < N; i++)
        {
            if (Random.Shared.NextDouble() < 0.5)
            {
                _hashTable.Add(i.ToString(), i);
            }
        }
    }
    
    [Benchmark]
    public int HashTableLookup()
    {
        var count = 0;
        for (var i = 0; i < N; i++)
        {
            if (_hashTable.TryGetValue(i.ToString(), out var value))
            {
                count++;
            }
        }

        return count;
    }
}
