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
| Method               | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------- |----------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| Rust_FastDate_Utf8   |  1.235 μs | 0.2020 μs | 0.5796 μs |  1.083 μs |  0.12 |    0.08 |    1 |         - |          NA |
| Rust_FastDate_String |  1.358 μs | 0.2099 μs | 0.5885 μs |  1.292 μs |  0.13 |    0.08 |    1 |         - |          NA |
| System_Utf8Parser    |  2.089 μs | 0.3471 μs | 0.9903 μs |  1.812 μs |  0.20 |    0.13 |    2 |         - |          NA |
| System_ParseExact    | 12.143 μs | 1.5306 μs | 4.4650 μs | 11.126 μs |  1.15 |    0.64 |    3 |         - |          NA |
| System_Parse         | 12.552 μs | 1.5853 μs | 4.3663 μs | 11.729 μs |  1.19 |    0.65 |    3 |         - |          NA |

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
