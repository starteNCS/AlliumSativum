using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;

namespace AlliumSativum.Performance.Utils;

[MemoryDiagnoser]
[CsvExporter]
[CsvMeasurementsExporter]
public class ProjectPerformanceTest
{
    private List<SourceTable> _data;

    [Params(1000)] 
    public int N;

    // Number of fields to project to (simulating SELECT field1, field2, ... FROM table)
    [Params(1, 3, 5, 10, 15, 20, 30, 50)]
    public int ProjectedFieldCount;

    [GlobalSetup]
    public void Setup()
    {
        _data = new List<SourceTable>(N);
        for (int i = 0; i < N; i++)
        {
            _data.Add(new SourceTable
            {
                Field1 = i, Field2 = i, Field3 = i, Field4 = i, Field5 = i,
                Field6 = i, Field7 = i, Field8 = i, Field9 = i, Field10 = i,
                Field11 = i, Field12 = i, Field13 = i, Field14 = i, Field15 = i,
                Field16 = i, Field17 = i, Field18 = i, Field19 = i, Field20 = i,
                Field21 = i, Field22 = i, Field23 = i, Field24 = i, Field25 = i,
                Field26 = i, Field27 = i, Field28 = i, Field29 = i, Field30 = i,
                Field31 = i, Field32 = i, Field33 = i, Field34 = i, Field35 = i,
                Field36 = i, Field37 = i, Field38 = i, Field39 = i, Field40 = i,
                Field41 = i, Field42 = i, Field43 = i, Field44 = i, Field45 = i,
                Field46 = i, Field47 = i, Field48 = i, Field49 = i, Field50 = i
            });
        }
    }

    [Benchmark(Baseline = true)]
    public object ForLoop()
    {
        return ProjectedFieldCount switch
        {
            1 => ForLoop1Field(),
            3 => ForLoop3Fields(),
            5 => ForLoop5Fields(),
            10 => ForLoop10Fields(),
            15 => ForLoop15Fields(),
            20 => ForLoop20Fields(),
            30 => ForLoop30Fields(),
            50 => ForLoop50Fields(),
            _ => throw new ArgumentException("Invalid ProjectedFieldCount")
        };
    }

    [Benchmark]
    public object LinqSelect()
    {
        return ProjectedFieldCount switch
        {
            1 => LinqSelect1Field(),
            3 => LinqSelect3Fields(),
            5 => LinqSelect5Fields(),
            10 => LinqSelect10Fields(),
            15 => LinqSelect15Fields(),
            20 => LinqSelect20Fields(),
            30 => LinqSelect30Fields(),
            50 => LinqSelect50Fields(),
            _ => throw new ArgumentException("Invalid ProjectedFieldCount")
        };
    }

    // For-Loop implementations
    private List<Projection1> ForLoop1Field()
    {
        var results = new List<Projection1>(N);
        for (int i = 0; i < _data.Count; i++)
        {
            results.Add(new Projection1 { Field1 = _data[i].Field1 });
        }
        return results;
    }

    private List<Projection3> ForLoop3Fields()
    {
        var results = new List<Projection3>(N);
        for (int i = 0; i < _data.Count; i++)
        {
            var src = _data[i];
            results.Add(new Projection3 
            { 
                Field1 = src.Field1, 
                Field2 = src.Field2, 
                Field3 = src.Field3 
            });
        }
        return results;
    }

    private List<Projection5> ForLoop5Fields()
    {
        var results = new List<Projection5>(N);
        for (int i = 0; i < _data.Count; i++)
        {
            var src = _data[i];
            results.Add(new Projection5 
            { 
                Field1 = src.Field1, Field2 = src.Field2, Field3 = src.Field3, 
                Field4 = src.Field4, Field5 = src.Field5 
            });
        }
        return results;
    }

    private List<Projection10> ForLoop10Fields()
    {
        var results = new List<Projection10>(N);
        for (int i = 0; i < _data.Count; i++)
        {
            var src = _data[i];
            results.Add(new Projection10 
            { 
                Field1 = src.Field1, Field2 = src.Field2, Field3 = src.Field3, Field4 = src.Field4, Field5 = src.Field5,
                Field6 = src.Field6, Field7 = src.Field7, Field8 = src.Field8, Field9 = src.Field9, Field10 = src.Field10
            });
        }
        return results;
    }

    private List<Projection15> ForLoop15Fields()
    {
        var results = new List<Projection15>(N);
        for (int i = 0; i < _data.Count; i++)
        {
            var src = _data[i];
            results.Add(new Projection15 
            { 
                Field1 = src.Field1, Field2 = src.Field2, Field3 = src.Field3, Field4 = src.Field4, Field5 = src.Field5,
                Field6 = src.Field6, Field7 = src.Field7, Field8 = src.Field8, Field9 = src.Field9, Field10 = src.Field10,
                Field11 = src.Field11, Field12 = src.Field12, Field13 = src.Field13, Field14 = src.Field14, Field15 = src.Field15
            });
        }
        return results;
    }

    private List<Projection20> ForLoop20Fields()
    {
        var results = new List<Projection20>(N);
        for (int i = 0; i < _data.Count; i++)
        {
            var src = _data[i];
            results.Add(new Projection20 
            { 
                Field1 = src.Field1, Field2 = src.Field2, Field3 = src.Field3, Field4 = src.Field4, Field5 = src.Field5,
                Field6 = src.Field6, Field7 = src.Field7, Field8 = src.Field8, Field9 = src.Field9, Field10 = src.Field10,
                Field11 = src.Field11, Field12 = src.Field12, Field13 = src.Field13, Field14 = src.Field14, Field15 = src.Field15,
                Field16 = src.Field16, Field17 = src.Field17, Field18 = src.Field18, Field19 = src.Field19, Field20 = src.Field20
            });
        }
        return results;
    }

    private List<Projection30> ForLoop30Fields()
    {
        var results = new List<Projection30>(N);
        for (int i = 0; i < _data.Count; i++)
        {
            var src = _data[i];
            results.Add(new Projection30 
            { 
                Field1 = src.Field1, Field2 = src.Field2, Field3 = src.Field3, Field4 = src.Field4, Field5 = src.Field5,
                Field6 = src.Field6, Field7 = src.Field7, Field8 = src.Field8, Field9 = src.Field9, Field10 = src.Field10,
                Field11 = src.Field11, Field12 = src.Field12, Field13 = src.Field13, Field14 = src.Field14, Field15 = src.Field15,
                Field16 = src.Field16, Field17 = src.Field17, Field18 = src.Field18, Field19 = src.Field19, Field20 = src.Field20,
                Field21 = src.Field21, Field22 = src.Field22, Field23 = src.Field23, Field24 = src.Field24, Field25 = src.Field25,
                Field26 = src.Field26, Field27 = src.Field27, Field28 = src.Field28, Field29 = src.Field29, Field30 = src.Field30
            });
        }
        return results;
    }

    private List<Projection50> ForLoop50Fields()
    {
        var results = new List<Projection50>(N);
        for (int i = 0; i < _data.Count; i++)
        {
            var src = _data[i];
            results.Add(new Projection50 
            { 
                Field1 = src.Field1, Field2 = src.Field2, Field3 = src.Field3, Field4 = src.Field4, Field5 = src.Field5,
                Field6 = src.Field6, Field7 = src.Field7, Field8 = src.Field8, Field9 = src.Field9, Field10 = src.Field10,
                Field11 = src.Field11, Field12 = src.Field12, Field13 = src.Field13, Field14 = src.Field14, Field15 = src.Field15,
                Field16 = src.Field16, Field17 = src.Field17, Field18 = src.Field18, Field19 = src.Field19, Field20 = src.Field20,
                Field21 = src.Field21, Field22 = src.Field22, Field23 = src.Field23, Field24 = src.Field24, Field25 = src.Field25,
                Field26 = src.Field26, Field27 = src.Field27, Field28 = src.Field28, Field29 = src.Field29, Field30 = src.Field30,
                Field31 = src.Field31, Field32 = src.Field32, Field33 = src.Field33, Field34 = src.Field34, Field35 = src.Field35,
                Field36 = src.Field36, Field37 = src.Field37, Field38 = src.Field38, Field39 = src.Field39, Field40 = src.Field40,
                Field41 = src.Field41, Field42 = src.Field42, Field43 = src.Field43, Field44 = src.Field44, Field45 = src.Field45,
                Field46 = src.Field46, Field47 = src.Field47, Field48 = src.Field48, Field49 = src.Field49, Field50 = src.Field50
            });
        }
        return results;
    }

    // LINQ Select implementations
    private List<Projection1> LinqSelect1Field()
    {
        return _data.Select(x => new Projection1 { Field1 = x.Field1 }).ToList();
    }

    private List<Projection3> LinqSelect3Fields()
    {
        return _data.Select(x => new Projection3 
        { 
            Field1 = x.Field1, 
            Field2 = x.Field2, 
            Field3 = x.Field3 
        }).ToList();
    }

    private List<Projection5> LinqSelect5Fields()
    {
        return _data.Select(x => new Projection5 
        { 
            Field1 = x.Field1, Field2 = x.Field2, Field3 = x.Field3, 
            Field4 = x.Field4, Field5 = x.Field5 
        }).ToList();
    }

    private List<Projection10> LinqSelect10Fields()
    {
        return _data.Select(x => new Projection10 
        { 
            Field1 = x.Field1, Field2 = x.Field2, Field3 = x.Field3, Field4 = x.Field4, Field5 = x.Field5,
            Field6 = x.Field6, Field7 = x.Field7, Field8 = x.Field8, Field9 = x.Field9, Field10 = x.Field10
        }).ToList();
    }

    private List<Projection15> LinqSelect15Fields()
    {
        return _data.Select(x => new Projection15 
        { 
            Field1 = x.Field1, Field2 = x.Field2, Field3 = x.Field3, Field4 = x.Field4, Field5 = x.Field5,
            Field6 = x.Field6, Field7 = x.Field7, Field8 = x.Field8, Field9 = x.Field9, Field10 = x.Field10,
            Field11 = x.Field11, Field12 = x.Field12, Field13 = x.Field13, Field14 = x.Field14, Field15 = x.Field15
        }).ToList();
    }

    private List<Projection20> LinqSelect20Fields()
    {
        return _data.Select(x => new Projection20 
        { 
            Field1 = x.Field1, Field2 = x.Field2, Field3 = x.Field3, Field4 = x.Field4, Field5 = x.Field5,
            Field6 = x.Field6, Field7 = x.Field7, Field8 = x.Field8, Field9 = x.Field9, Field10 = x.Field10,
            Field11 = x.Field11, Field12 = x.Field12, Field13 = x.Field13, Field14 = x.Field14, Field15 = x.Field15,
            Field16 = x.Field16, Field17 = x.Field17, Field18 = x.Field18, Field19 = x.Field19, Field20 = x.Field20
        }).ToList();
    }

    private List<Projection30> LinqSelect30Fields()
    {
        return _data.Select(x => new Projection30 
        { 
            Field1 = x.Field1, Field2 = x.Field2, Field3 = x.Field3, Field4 = x.Field4, Field5 = x.Field5,
            Field6 = x.Field6, Field7 = x.Field7, Field8 = x.Field8, Field9 = x.Field9, Field10 = x.Field10,
            Field11 = x.Field11, Field12 = x.Field12, Field13 = x.Field13, Field14 = x.Field14, Field15 = x.Field15,
            Field16 = x.Field16, Field17 = x.Field17, Field18 = x.Field18, Field19 = x.Field19, Field20 = x.Field20,
            Field21 = x.Field21, Field22 = x.Field22, Field23 = x.Field23, Field24 = x.Field24, Field25 = x.Field25,
            Field26 = x.Field26, Field27 = x.Field27, Field28 = x.Field28, Field29 = x.Field29, Field30 = x.Field30
        }).ToList();
    }

    private List<Projection50> LinqSelect50Fields()
    {
        return _data.Select(x => new Projection50 
        { 
            Field1 = x.Field1, Field2 = x.Field2, Field3 = x.Field3, Field4 = x.Field4, Field5 = x.Field5,
            Field6 = x.Field6, Field7 = x.Field7, Field8 = x.Field8, Field9 = x.Field9, Field10 = x.Field10,
            Field11 = x.Field11, Field12 = x.Field12, Field13 = x.Field13, Field14 = x.Field14, Field15 = x.Field15,
            Field16 = x.Field16, Field17 = x.Field17, Field18 = x.Field18, Field19 = x.Field19, Field20 = x.Field20,
            Field21 = x.Field21, Field22 = x.Field22, Field23 = x.Field23, Field24 = x.Field24, Field25 = x.Field25,
            Field26 = x.Field26, Field27 = x.Field27, Field28 = x.Field28, Field29 = x.Field29, Field30 = x.Field30,
            Field31 = x.Field31, Field32 = x.Field32, Field33 = x.Field33, Field34 = x.Field34, Field35 = x.Field35,
            Field36 = x.Field36, Field37 = x.Field37, Field38 = x.Field38, Field39 = x.Field39, Field40 = x.Field40,
            Field41 = x.Field41, Field42 = x.Field42, Field43 = x.Field43, Field44 = x.Field44, Field45 = x.Field45,
            Field46 = x.Field46, Field47 = x.Field47, Field48 = x.Field48, Field49 = x.Field49, Field50 = x.Field50
        }).ToList();
    }

    // Source "table" with 50 fields
    private class SourceTable
    {
        public int Field1 { get; set; }
        public int Field2 { get; set; }
        public int Field3 { get; set; }
        public int Field4 { get; set; }
        public int Field5 { get; set; }
        public int Field6 { get; set; }
        public int Field7 { get; set; }
        public int Field8 { get; set; }
        public int Field9 { get; set; }
        public int Field10 { get; set; }
        public int Field11 { get; set; }
        public int Field12 { get; set; }
        public int Field13 { get; set; }
        public int Field14 { get; set; }
        public int Field15 { get; set; }
        public int Field16 { get; set; }
        public int Field17 { get; set; }
        public int Field18 { get; set; }
        public int Field19 { get; set; }
        public int Field20 { get; set; }
        public int Field21 { get; set; }
        public int Field22 { get; set; }
        public int Field23 { get; set; }
        public int Field24 { get; set; }
        public int Field25 { get; set; }
        public int Field26 { get; set; }
        public int Field27 { get; set; }
        public int Field28 { get; set; }
        public int Field29 { get; set; }
        public int Field30 { get; set; }
        public int Field31 { get; set; }
        public int Field32 { get; set; }
        public int Field33 { get; set; }
        public int Field34 { get; set; }
        public int Field35 { get; set; }
        public int Field36 { get; set; }
        public int Field37 { get; set; }
        public int Field38 { get; set; }
        public int Field39 { get; set; }
        public int Field40 { get; set; }
        public int Field41 { get; set; }
        public int Field42 { get; set; }
        public int Field43 { get; set; }
        public int Field44 { get; set; }
        public int Field45 { get; set; }
        public int Field46 { get; set; }
        public int Field47 { get; set; }
        public int Field48 { get; set; }
        public int Field49 { get; set; }
        public int Field50 { get; set; }
    }

    // Projection classes (like SELECT field1, field2, ... FROM table)
    private class Projection1
    {
        public int Field1 { get; set; }
    }

    private class Projection3
    {
        public int Field1 { get; set; }
        public int Field2 { get; set; }
        public int Field3 { get; set; }
    }

    private class Projection5
    {
        public int Field1 { get; set; }
        public int Field2 { get; set; }
        public int Field3 { get; set; }
        public int Field4 { get; set; }
        public int Field5 { get; set; }
    }

    private class Projection10
    {
        public int Field1 { get; set; }
        public int Field2 { get; set; }
        public int Field3 { get; set; }
        public int Field4 { get; set; }
        public int Field5 { get; set; }
        public int Field6 { get; set; }
        public int Field7 { get; set; }
        public int Field8 { get; set; }
        public int Field9 { get; set; }
        public int Field10 { get; set; }
    }

    private class Projection15
    {
        public int Field1 { get; set; }
        public int Field2 { get; set; }
        public int Field3 { get; set; }
        public int Field4 { get; set; }
        public int Field5 { get; set; }
        public int Field6 { get; set; }
        public int Field7 { get; set; }
        public int Field8 { get; set; }
        public int Field9 { get; set; }
        public int Field10 { get; set; }
        public int Field11 { get; set; }
        public int Field12 { get; set; }
        public int Field13 { get; set; }
        public int Field14 { get; set; }
        public int Field15 { get; set; }
    }

    private class Projection20
    {
        public int Field1 { get; set; }
        public int Field2 { get; set; }
        public int Field3 { get; set; }
        public int Field4 { get; set; }
        public int Field5 { get; set; }
        public int Field6 { get; set; }
        public int Field7 { get; set; }
        public int Field8 { get; set; }
        public int Field9 { get; set; }
        public int Field10 { get; set; }
        public int Field11 { get; set; }
        public int Field12 { get; set; }
        public int Field13 { get; set; }
        public int Field14 { get; set; }
        public int Field15 { get; set; }
        public int Field16 { get; set; }
        public int Field17 { get; set; }
        public int Field18 { get; set; }
        public int Field19 { get; set; }
        public int Field20 { get; set; }
    }

    private class Projection30
    {
        public int Field1 { get; set; }
        public int Field2 { get; set; }
        public int Field3 { get; set; }
        public int Field4 { get; set; }
        public int Field5 { get; set; }
        public int Field6 { get; set; }
        public int Field7 { get; set; }
        public int Field8 { get; set; }
        public int Field9 { get; set; }
        public int Field10 { get; set; }
        public int Field11 { get; set; }
        public int Field12 { get; set; }
        public int Field13 { get; set; }
        public int Field14 { get; set; }
        public int Field15 { get; set; }
        public int Field16 { get; set; }
        public int Field17 { get; set; }
        public int Field18 { get; set; }
        public int Field19 { get; set; }
        public int Field20 { get; set; }
        public int Field21 { get; set; }
        public int Field22 { get; set; }
        public int Field23 { get; set; }
        public int Field24 { get; set; }
        public int Field25 { get; set; }
        public int Field26 { get; set; }
        public int Field27 { get; set; }
        public int Field28 { get; set; }
        public int Field29 { get; set; }
        public int Field30 { get; set; }
    }

    private class Projection50
    {
        public int Field1 { get; set; }
        public int Field2 { get; set; }
        public int Field3 { get; set; }
        public int Field4 { get; set; }
        public int Field5 { get; set; }
        public int Field6 { get; set; }
        public int Field7 { get; set; }
        public int Field8 { get; set; }
        public int Field9 { get; set; }
        public int Field10 { get; set; }
        public int Field11 { get; set; }
        public int Field12 { get; set; }
        public int Field13 { get; set; }
        public int Field14 { get; set; }
        public int Field15 { get; set; }
        public int Field16 { get; set; }
        public int Field17 { get; set; }
        public int Field18 { get; set; }
        public int Field19 { get; set; }
        public int Field20 { get; set; }
        public int Field21 { get; set; }
        public int Field22 { get; set; }
        public int Field23 { get; set; }
        public int Field24 { get; set; }
        public int Field25 { get; set; }
        public int Field26 { get; set; }
        public int Field27 { get; set; }
        public int Field28 { get; set; }
        public int Field29 { get; set; }
        public int Field30 { get; set; }
        public int Field31 { get; set; }
        public int Field32 { get; set; }
        public int Field33 { get; set; }
        public int Field34 { get; set; }
        public int Field35 { get; set; }
        public int Field36 { get; set; }
        public int Field37 { get; set; }
        public int Field38 { get; set; }
        public int Field39 { get; set; }
        public int Field40 { get; set; }
        public int Field41 { get; set; }
        public int Field42 { get; set; }
        public int Field43 { get; set; }
        public int Field44 { get; set; }
        public int Field45 { get; set; }
        public int Field46 { get; set; }
        public int Field47 { get; set; }
        public int Field48 { get; set; }
        public int Field49 { get; set; }
        public int Field50 { get; set; }
    }
}
