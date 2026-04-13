using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FastDate;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class DateParsingBenchmarks
{
    private string[] _dateStrings = null!;
    private byte[][] _utf8Dates = null!;
    private const int Iterations = 1000;

    [GlobalSetup]
    public void Setup()
    {
        // Generate a variety of dates to prevent CPU branch prediction bias
        _dateStrings = Enumerable.Range(0, Iterations)
            .Select(i => DateTime.UtcNow.AddDays(i).ToString("yyyy-MM-dd'T'HH:mm:ss"))
            .ToArray();

        _utf8Dates = _dateStrings.Select(System.Text.Encoding.UTF8.GetBytes).ToArray();
    }

    private string GetNextString() => _dateStrings[_index++ % Iterations];
    private ReadOnlySpan<byte> GetNextUtf8() => _utf8Dates[_index++ % Iterations];

    [Benchmark(Baseline = true)]
    public DateTime System_ParseExact()
    {
        return DateTime.ParseExact(GetNextString(), "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private int _index;

    [IterationSetup]
    public void IterationSetup() => _index = 0;

    [Benchmark]
    public DateTime System_Utf8Parser()
    {
        if (System.Buffers.Text.Utf8Parser.TryParse(GetNextUtf8(), out DateTime dt, out _))
        {
            return dt;
        }
        return DateTime.MinValue;
    }

    [Benchmark]
    public DateTime Rust_FastDate_Utf8()
    {
        return FastDate.FastDate.FromIso8601(GetNextUtf8());
    }
    
    [Benchmark]
    public DateTime Rust_FastDate_String()
    {
        return FastDate.FastDate.FromIso8601(GetNextString());
    }
}

public static class Program
{
    public static void Main(string[] args)
    { 
        BenchmarkRunner.Run<DateParsingBenchmarks>();
    }
}