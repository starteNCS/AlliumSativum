using BenchmarkDotNet.Attributes;

namespace AlliumSativum.Performance.Utils;

[MemoryDiagnoser]
[CsvExporter]
[CsvMeasurementsExporter]
public class ProjectPerformanceTest
{
    private const int MaxFieldCount = 50;
    private const int MaxRowCount = 100_000;

    private List<Dictionary<string, object>> _data;
    private List<string> _fieldNames;

    [Params(1, 10, 100, 1_000, 10_000, MaxRowCount)]
    public int N;

    // Number of fields to project to (simulating SELECT field1, field2, ... FROM table)
    [Params(1, 3, 5, 10, 15, 20, 30, MaxFieldCount)]
    public int ProjectedFieldCount;

    [GlobalSetup]
    public void Setup()
    {
        _data = new List<Dictionary<string, object>>(N);
        for (var i = 0; i < MaxRowCount; i++)
        {
            var dict = new Dictionary<string, object>();
            for (var j = 0; j < MaxFieldCount; j++) dict.Add(j.ToString(), j);
            _data.Add(dict);
        }

        _fieldNames = Enumerable.Range(0, ProjectedFieldCount).Select(j => j.ToString()).ToList();
    }

    [Benchmark]
    public List<Dictionary<string, object>> Project()
    {
        var results = new List<Dictionary<string, object>>();
        foreach (var item in _data)
        {
            var projected = new Dictionary<string, object>(N);
            foreach (var propName in _fieldNames) projected[propName] = item[propName];
            results.Add(projected);
        }

        return results;
    }
}