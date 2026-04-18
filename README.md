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
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                     | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| Rust_FastDate_Bulk_Strings |   5.696 ns | 0.0090 ns | 0.0080 ns |  0.12 |    0.00 |    1 |         - |          NA |
| Rust_FastDate_Utf8         |   8.883 ns | 0.0173 ns | 0.0162 ns |  0.19 |    0.00 |    2 |         - |          NA |
| Rust_FastDate_String       |  11.174 ns | 0.1029 ns | 0.1408 ns |  0.24 |    0.00 |    3 |         - |          NA |
| System_Utf8Parser          |  18.878 ns | 0.0650 ns | 0.0507 ns |  0.40 |    0.00 |    4 |         - |          NA |
| System_ParseExact          |  47.239 ns | 0.1483 ns | 0.1238 ns |  1.00 |    0.00 |    5 |         - |          NA |
| System_Parse               | 156.844 ns | 2.9838 ns | 3.4362 ns |  3.32 |    0.07 |    6 |         - |          NA |

## Supported Platforms
- Apple ARM64 (NEON)
- Windows x64 (SSE3)
- Linux x64 (SSE3)

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
