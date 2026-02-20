using BenchmarkDotNet.Attributes;

namespace AlliumSativum.Performance.Utils;

[MemoryDiagnoser]
[CsvExporter]
[CsvMeasurementsExporter]
public class SingleFilterDoublePerformanceTest
{
    [Params(100, 1_000, 10_000, 20_000, 30_000)]
    public double N;
    
    [Benchmark]
    public int IsEqual()
    {
        var counter = 0;
        for(double i = 0; i < N; i++)
        {
            if(i == 500)
            {
                counter++;
            }
        }
        return counter;
    }
    
    [Benchmark]
    public int IsNotEqual()
    {
        var counter = 0;
        for(double i = 0; i < N; i++)
        {
            if(i != 500)
            {
                counter++;
            }
        }
        return counter;
    }
    
    [Benchmark]
    public int IsGreater()
    {
        var counter = 0;
        for(double i = 0; i < N; i++)
        {
            if(i > 500)
            {
                counter++;
            }
        }
        return counter;
    }
    
    [Benchmark]
    public int IsGreaterOrEqual()
    {
        var counter = 0;
        for(double i = 0; i < N; i++)
        {
            if(i >= 500)
            {
                counter++;
            }
        }
        return counter;
    }
    
    [Benchmark]
    public int IsLower()
    {
        var counter = 0;
        for(double i = 0; i < N; i++)
        {
            if(i < 500)
            {
                counter++;
            }
        }
        return counter;
    }
    
    [Benchmark]
    public int IsLowerOrEqual()
    {
        var counter = 0;
        for(double i = 0; i < N; i++)
        {
            if (i <= 500)
            {
                counter++;
            }
        }

        return counter;
    }
}
