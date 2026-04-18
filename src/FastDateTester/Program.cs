using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FastDate;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class DateParsingBenchmarks
{
    private List<string> _dateStrings = null!;
    private byte[][] _utf8Dates = null!;
    private const int ITERATIONS = 1_000;

    [GlobalSetup]
    public void Setup()
    {
        // Generate a variety of dates to prevent CPU branch prediction bias
        _dateStrings = Enumerable.Range(0, ITERATIONS)
            .Select(i => DateTime.UtcNow.AddDays(i).ToString("O"))
            .ToList();

        _utf8Dates = _dateStrings.Select(System.Text.Encoding.UTF8.GetBytes).ToArray();
        _bulkOutputs = new PackedDateTime[ITERATIONS];
    }

    private PackedDateTime[] _bulkOutputs = null!;

    [Benchmark(Baseline = true, OperationsPerInvoke = ITERATIONS)]
    public int System_ParseExact()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum += DateTime.ParseExact(_dateStrings[i], "O", CultureInfo.InvariantCulture).Day;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS)]
    public int System_Parse()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum += DateTime.Parse(_dateStrings[i], CultureInfo.InvariantCulture).Day;
        }
        return sum;
    }
    
    [Benchmark(OperationsPerInvoke = ITERATIONS)]
    public int System_Utf8Parser()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            if (System.Buffers.Text.Utf8Parser.TryParse(_utf8Dates[i], out DateTime dt, out _, 'O'))
            {
                sum += dt.Day;
            }
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS)]
    public int Rust_FastDate_Utf8()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum += FastDate.Parser.FromIso8601(_utf8Dates[i]).Day();
        }
        return sum;
    }
    
    [Benchmark(OperationsPerInvoke = ITERATIONS)]
    public int Rust_FastDate_String()
    {
        int sum = 0;
        for (int i = 0; i < ITERATIONS; i++)
        {
            sum += FastDate.Parser.FromIso8601(_dateStrings[i]).Day();
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS)]
    public void Rust_FastDate_Bulk_Strings()
    {
        FastDate.Parser.FromIso8601Bulk(_dateStrings, _bulkOutputs);
    }
}

public static class Program
{
    public static void Main(string[] args)
    { 
        BenchmarkRunner.Run<DateParsingBenchmarks>();
    }
}