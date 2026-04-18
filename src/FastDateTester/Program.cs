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
    private const int ITERATIONS = 1_000;

    [GlobalSetup]
    public void Setup()
    {
        // Generate a variety of dates to prevent CPU branch prediction bias
        _dateStrings = Enumerable.Range(0, ITERATIONS)
            .Select(i => DateTime.UtcNow.AddDays(i).ToString("O"))
            .ToArray();

        _utf8Dates = _dateStrings.Select(System.Text.Encoding.UTF8.GetBytes).ToArray();
    }

    private string GetNextString() => _dateStrings[_index++ % ITERATIONS];
    private ReadOnlySpan<byte> GetNextUtf8() => _utf8Dates[_index++ % ITERATIONS];

    [Benchmark(Baseline = true)]
    public DateTime System_ParseExact()
    {
        return DateTime.ParseExact(GetNextString(), "O", CultureInfo.InvariantCulture);
    }

    private int _index;

    [IterationSetup]
    public void IterationSetup() => _index = 0;

    [Benchmark]
    public DateTime System_Utf8Parser()
    {
        if (System.Buffers.Text.Utf8Parser.TryParse(GetNextUtf8(), out DateTime dt, out _, 'O'))
        {
            return dt;
        }
        
        throw new FormatException();
    }

    [Benchmark]
    public DateTime Rust_FastDate_Utf8()
    {
        return FastDate.Parser.FromIso8601(GetNextUtf8()).ToDateTime();
    }
    
    [Benchmark]
    public DateTime Rust_FastDate_String()
    {
        return FastDate.Parser.FromIso8601(GetNextString()).ToDateTime();
    }
}

public static class Program
{
    public static void Main(string[] args)
    { 
        BenchmarkRunner.Run<DateParsingBenchmarks>();
    }
}