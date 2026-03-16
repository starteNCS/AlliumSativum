using AlliumSativum.Shared.Models.IntermediateModels.Expressions;
using AlliumSativum.Shared.Models.IntermediateModels.Specifiers;
using BenchmarkDotNet.Attributes;

namespace AlliumSativum.Performance.Utils;

[MemoryDiagnoser]
[CsvExporter]
[CsvMeasurementsExporter]
public class SingleFilterDoublePerformanceTest
{
    public static readonly BinaryOperatorExpressionNode expression = new()
    {
        Left = new FullySpecifiedColumnExpressionNode
        {
            Attribute = new AttributeSpecifier("cs", "experiment_run", "benchmark_id")
        },
        Operation = "replace in test",
        Right = new ValueExpressionNode
        {
            Value = "500",
            Type = ValueExpressionNode.ValueExpressionType.Numeric
        }
    };

    private List<Dictionary<string, object>> _data;

    [Params(1, 10, 100, 1000)] public double N;

    [GlobalSetup]
    public void Setup()
    {
        _data = [];
        for (double i = 0; i < N; i++)
            _data.Add(new Dictionary<string, object>
            {
                ["cs->experiment_run.benchmark_id"] = i
            });
    }

    [Benchmark]
    public int IsEqual()
    {
        expression.Operation = "=";
        var counter = 0;
        foreach (var item in _data)
            if (expression.EvaluatePredicate(item))
                counter++;

        return counter;
    }

    [Benchmark]
    public int IsNotEqual()
    {
        expression.Operation = "!=";
        var counter = 0;
        foreach (var item in _data)
            if (expression.EvaluatePredicate(item))
                counter++;

        return counter;
    }

    [Benchmark]
    public int IsGreater()
    {
        expression.Operation = ">";
        var counter = 0;
        foreach (var item in _data)
            if (expression.EvaluatePredicate(item))
                counter++;

        return counter;
    }

    [Benchmark]
    public int IsGreaterOrEqual()
    {
        expression.Operation = ">=";
        var counter = 0;
        foreach (var item in _data)
            if (expression.EvaluatePredicate(item))
                counter++;

        return counter;
    }

    [Benchmark]
    public int IsLower()
    {
        expression.Operation = "<";
        var counter = 0;
        foreach (var item in _data)
            if (expression.EvaluatePredicate(item))
                counter++;

        return counter;
    }

    [Benchmark]
    public int IsLowerOrEqual()
    {
        expression.Operation = "<=";
        var counter = 0;
        foreach (var item in _data)
            if (expression.EvaluatePredicate(item))
                counter++;

        return counter;
    }
}