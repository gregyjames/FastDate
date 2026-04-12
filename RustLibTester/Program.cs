using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace RustLib;

[MemoryDiagnoser]
public class DateParsingBenchmarks
{
    private string _dateStr = null!;

    [GlobalSetup]
    public void Setup()
    {
        _dateStr = "2026-04-12T15:04:05";
    }

    [Benchmark]
    public DateTime RustFastDate()
    {
        return FastDate.FromISO8601(_dateStr);
    }

    [Benchmark]
    public DateTime ParseExact()
    {
        return DateTime.ParseExact(
            _dateStr,
            "yyyy-MM-dd'T'HH:mm:ss",
            CultureInfo.InvariantCulture
        );
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<DateParsingBenchmarks>();
    }
}