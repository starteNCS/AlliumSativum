using AlliumSativum.Performance.Utils;
using BenchmarkDotNet.Running;

var summary = BenchmarkRunner.Run<SingleFilterDoublePerformanceTest>();