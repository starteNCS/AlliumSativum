using BenchmarkDotNet.Attributes;

namespace AlliumSativum.Performance.Utils;

[MemoryDiagnoser]
[CsvExporter]
[CsvMeasurementsExporter]
public class SingleFilterStringPerformanceTest
{
    private List<string> _data;
    private string _target = "SearchTarget_500"; // Mid-list target

    [Params(100, 1_000, 10_000, 20_000, 30_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        // Creating a list of strings to simulate a DB column
        _data = Enumerable.Range(0, N)
            .Select(i => $"SearchTarget_{i}")
            .ToList();
    }

    [Benchmark]
    public int EqualsOrdinal()
    {
        var counter = 0;
        for (int i = 0; i < _data.Count; i++)
        {
            if (string.Equals(_data[i], _target, StringComparison.Ordinal))
            {
                counter++;
            }
        }

        return counter;
    }
    
    [Benchmark]
    public int NotEqualsOrdinal()
    {
        var counter = 0;
        for (int i = 0; i < _data.Count; i++)
        {
            if(!string.Equals(_data[i], _target, StringComparison.Ordinal))
            {
                counter++;
            }
        }
        
        return counter;
    }
}
