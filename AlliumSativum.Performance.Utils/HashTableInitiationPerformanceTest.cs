using BenchmarkDotNet.Attributes;

namespace AlliumSativum.Performance.Utils;

[MemoryDiagnoser]
[CsvExporter]
[CsvMeasurementsExporter]
public class HashTableInitiationPerformanceTest
{
    private Dictionary<string, object> _hashTable;
    
    [Params(1, 10, 100, 1000)]
    public double N;

    [IterationSetup]
    public void Setup()
    {
        _hashTable = new Dictionary<string, object>();
    }
    
    [Benchmark]
    public void HashTableInitiation()
    {
        for (var i = 0; i < N; i++)
        {
            _hashTable.Add(i.ToString(), i);
        }
    }
}
