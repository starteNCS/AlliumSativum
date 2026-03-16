using BenchmarkDotNet.Attributes;

namespace AlliumSativum.Performance.Utils;

[MemoryDiagnoser]
[CsvExporter]
[CsvMeasurementsExporter]
public class ProjectionCardinalityPerformanceTest
{
    private List<SourceTable> _cardData;
    private List<SourceTable> _data;

    [Params(1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1000)]
    public int Card;

    [Params(1000)] public int N;

    [GlobalSetup]
    public void Setup()
    {
        _data = new List<SourceTable>(N);
        for (var i = 0; i < N; i++)
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

    [IterationSetup]
    public void Pre()
    {
        _cardData = _data.Take(Card).ToList();
    }

    [Benchmark]
    public object LinqSelect()
    {
        return _cardData.Select(x => new Projection10
        {
            Field1 = x.Field1,
            Field2 = x.Field2,
            Field3 = x.Field3,
            Field4 = x.Field4,
            Field5 = x.Field5,
            Field6 = x.Field6,
            Field7 = x.Field7,
            Field8 = x.Field8,
            Field9 = x.Field9,
            Field10 = x.Field10
        }).ToList();
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
}