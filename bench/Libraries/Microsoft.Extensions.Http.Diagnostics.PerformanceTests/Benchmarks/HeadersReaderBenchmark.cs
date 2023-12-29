// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Http.Logging.Internal;
using Microsoft.Extensions.Telemetry.Internal;

namespace Microsoft.Extensions.Http.Diagnostics.Bench.Benchmarks;

public class HeadersReaderBenchmark
{
    private List<KeyValuePair<string, string>> _outputBuffer = null!;
    private HttpHeadersReader _headersReader = null!;

    [Params(0, 5, 15)]
    public int HeadersCount { get; set; }

    // This one can't be 0, because in that case HttpRequestReader simply doesn't call HttpHeadersReader
    [Params(1, 3, 5, 10)]
    public int HeadersToLogCount { get; set; }

    public HttpRequestMessage? Request { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _outputBuffer = new(capacity: 10240);
        Request = new HttpRequestMessage(HttpMethod.Post, "https://www.microsoft.com");
        for (var i = 0; i < HeadersCount; i++)
        {
            Request.Headers.Add($"Header{i}", $"Value{i}");
        }

        var options = new LoggingOptions();
        for (var i = 0; i < HeadersToLogCount; i++)
        {
            options.RequestHeadersDataClasses.Add($"Header{i}", FakeTaxonomy.PublicData);
        }

        var redactor = new HttpHeadersRedactor(ErasingRedactorProvider.Instance);
        _headersReader = new HttpHeadersReader(new StaticOptionsMonitor<LoggingOptions>(options), redactor);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Request?.Dispose();
        _outputBuffer.Clear();
        _outputBuffer = null!;
    }

    [Benchmark]
    public void HeadersReaderOld()
    {
        _headersReader.ReadRequestHeaders(Request!, _outputBuffer);
    }

    [Benchmark]
    public void HeadersReaderNew()
    {
        _headersReader.ReadRequestHeadersNew(Request!, _outputBuffer);
    }
}

/*

BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22631.2861), VM=Hyper-V
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=8.0.100
  [Host] : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

Job=MediumRun  Jit=RyuJit  Platform=X64
Runtime=.NET 8.0  Server=True  Toolchain=InProcessEmitToolchain
IterationCount=15  LaunchCount=2  WarmupCount=10

|           Method | HeadersCount | HeadersToLogCount |        Mean |     Error |     StdDev |      Median |   Gen0 |   Gen1 | Allocated |
|----------------- |------------- |------------------ |------------:|----------:|-----------:|------------:|-------:|-------:|----------:|
| HeadersReaderOld |            0 |                 1 |    18.90 ns |  0.071 ns |   0.100 ns |    18.89 ns |      - |      - |         - |
| HeadersReaderOld |            0 |                 3 |    41.60 ns |  0.464 ns |   0.665 ns |    41.33 ns |      - |      - |         - |
| HeadersReaderOld |            0 |                 5 |    64.16 ns |  0.655 ns |   0.919 ns |    63.49 ns |      - |      - |         - |
| HeadersReaderOld |            0 |                10 |   120.26 ns |  0.276 ns |   0.377 ns |   120.25 ns |      - |      - |         - |
| HeadersReaderOld |            5 |                 1 |   402.43 ns |  6.976 ns |   9.549 ns |   402.17 ns | 0.0114 | 0.0057 |     296 B |
| HeadersReaderOld |            5 |                 3 | 1,341.08 ns | 71.636 ns | 100.423 ns | 1,337.73 ns | 0.0343 | 0.0172 |     888 B |
| HeadersReaderOld |            5 |                 5 | 2,189.89 ns | 74.132 ns | 106.319 ns | 2,190.03 ns | 0.0572 | 0.0267 |    1480 B |
| HeadersReaderOld |            5 |                10 | 2,349.91 ns | 23.901 ns |  32.716 ns | 2,351.23 ns | 0.0572 | 0.0267 |    1480 B |
| HeadersReaderOld |           15 |                 1 |   408.09 ns |  5.997 ns |   8.209 ns |   407.62 ns | 0.0114 | 0.0057 |     296 B |
| HeadersReaderOld |           15 |                 3 | 1,224.46 ns | 19.016 ns |  25.385 ns | 1,214.62 ns | 0.0343 | 0.0172 |     888 B |
| HeadersReaderOld |           15 |                 5 | 2,081.42 ns | 47.198 ns |  66.165 ns | 2,065.93 ns | 0.0572 | 0.0267 |    1480 B |
| HeadersReaderOld |           15 |                10 | 4,385.35 ns | 77.832 ns | 109.109 ns | 4,362.06 ns | 0.1144 | 0.0534 |    2960 B |

| HeadersReaderNew |            0 |                 1 |    10.40 ns |  0.013 ns |   0.018 ns |    10.40 ns |      - |      - |         - |
| HeadersReaderNew |            0 |                 5 |    10.44 ns |  0.029 ns |   0.041 ns |    10.43 ns |      - |      - |         - |
| HeadersReaderNew |            0 |                 3 |    10.69 ns |  0.216 ns |   0.295 ns |    10.69 ns |      - |      - |         - |
| HeadersReaderNew |            0 |                10 |    10.71 ns |  0.072 ns |   0.103 ns |    10.74 ns |      - |      - |         - |
| HeadersReaderNew |            5 |                 1 |   749.80 ns | 17.496 ns |  23.949 ns |   743.08 ns | 0.0191 | 0.0048 |     480 B |
| HeadersReaderNew |            5 |                 3 | 1,460.32 ns | 18.964 ns |  25.316 ns | 1,456.27 ns | 0.0401 | 0.0134 |    1008 B |
| HeadersReaderNew |            5 |                 5 | 2,149.69 ns | 28.055 ns |  38.403 ns | 2,147.51 ns | 0.0610 | 0.0305 |    1536 B |
| HeadersReaderNew |            5 |                10 | 2,087.11 ns | 22.876 ns |  31.313 ns | 2,086.37 ns | 0.0610 | 0.0305 |    1536 B |
| HeadersReaderNew |           15 |                 1 | 1,404.72 ns |  7.300 ns |   9.745 ns | 1,403.54 ns | 0.0305 | 0.0076 |     800 B |
| HeadersReaderNew |           15 |                 3 | 2,199.79 ns | 28.790 ns |  39.407 ns | 2,191.87 ns | 0.0496 | 0.0153 |    1328 B |
| HeadersReaderNew |           15 |                 5 | 2,967.58 ns | 24.105 ns |  32.995 ns | 2,962.05 ns | 0.0725 | 0.0229 |    1856 B |
| HeadersReaderNew |           15 |                10 | 4,418.11 ns | 35.622 ns |  47.555 ns | 4,408.71 ns | 0.1221 | 0.0610 |    3176 B |

|           Method | HeadersCount | HeadersToLogCount |         Mean |      Error |     StdDev |       Median |   Gen0 | Allocated |
|----------------- |------------- |------------------ |-------------:|-----------:|-----------:|-------------:|-------:|----------:|
| HeadersReaderNew |            0 |                 3 |     4.451 ns |  0.0070 ns |  0.0098 ns |     4.449 ns |      - |         - |
| HeadersReaderNew |            0 |                 1 |     4.523 ns |  0.0114 ns |  0.0167 ns |     4.526 ns |      - |         - |
| HeadersReaderNew |            0 |                10 |     4.597 ns |  0.0719 ns |  0.1007 ns |     4.659 ns |      - |         - |
| HeadersReaderNew |            0 |                 5 |     4.942 ns |  0.3811 ns |  0.5466 ns |     4.464 ns |      - |         - |
| HeadersReaderNew |            5 |                 1 |   205.183 ns |  1.4687 ns |  1.9607 ns |   205.608 ns | 0.0095 |     256 B |
| HeadersReaderNew |           15 |                 1 |   205.827 ns |  1.6307 ns |  2.1769 ns |   206.584 ns | 0.0095 |     256 B |
| HeadersReaderNew |           15 |                 3 |   613.793 ns |  4.6416 ns |  6.3535 ns |   613.704 ns | 0.0305 |     768 B |
| HeadersReaderNew |            5 |                 3 |   621.074 ns |  5.4308 ns |  7.4337 ns |   620.212 ns | 0.0305 |     768 B |
| HeadersReaderNew |            5 |                10 |   972.646 ns |  8.2481 ns | 11.2900 ns |   971.652 ns | 0.0515 |    1336 B |
| HeadersReaderNew |           15 |                 5 | 1,086.556 ns |  9.6457 ns | 13.2032 ns | 1,085.646 ns | 0.0496 |    1280 B |
| HeadersReaderNew |            5 |                 5 | 1,088.187 ns | 14.6687 ns | 19.5823 ns | 1,086.777 ns | 0.0496 |    1280 B |
| HeadersReaderNew |           15 |                10 | 2,367.874 ns | 16.3909 ns | 22.4361 ns | 2,359.508 ns | 0.0992 |    2560 B |

 */
