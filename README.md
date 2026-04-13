[![CI](https://github.com/gregyjames/FastDate/actions/workflows/build.yml/badge.svg)](https://github.com/gregyjames/HyperTrie/actions/workflows/ci.yml)
![NuGet Downloads](https://img.shields.io/nuget/dt/FastDate)
![NuGet Version](https://img.shields.io/nuget/v/FastDate)
[![codecov](https://codecov.io/github/gregyjames/FastDate/graph/badge.svg?token=2FY6SXTEQX)](https://codecov.io/github/gregyjames/FastDate)
![Rust](https://img.shields.io/badge/rust-%23000000.svg?style=for-the-badge&logo=rust&logoColor=white)
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)

# FastDate

FastDate is a SIMD vectorized datetime (ISO 8601) parser that is ~7x faster (86% less time) than DateTime.Parse()/ParseExact().

## Usage 
```csharp
FastDate.FromISO8601("2026-04-12T15:04:05")
```
## Benchmark
```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.1 (25B78) [Darwin 25.1.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  Job-CNUJVU : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a

InvocationCount=1  UnrollFactor=1  

```
| Method               | Mean      | Error     | StdDev    | Median      | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------- |----------:|----------:|----------:|------------:|------:|--------:|-----:|----------:|------------:|
| Rust_FastDate_Utf8   |  46.79 ns |  30.56 ns |  85.18 ns |   0.0000 ns |  0.07 |    0.13 |    1 |         - |          NA |
| Rust_FastDate_String |  64.10 ns |  18.08 ns |  48.56 ns |  41.0000 ns |  0.10 |    0.08 |    2 |         - |          NA |
| System_Utf8Parser    | 125.42 ns |  48.54 ns | 133.71 ns |  82.0000 ns |  0.19 |    0.22 |    2 |         - |          NA |
| System_ParseExact    | 732.60 ns | 102.38 ns | 283.70 ns | 604.5000 ns |  1.11 |    0.54 |    3 |         - |          NA |

## Limitations
- Currently only Supports Apple ARM64 (neon) but more to come!

## License
MIT License

Copyright (c) 2026 Greg James

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
