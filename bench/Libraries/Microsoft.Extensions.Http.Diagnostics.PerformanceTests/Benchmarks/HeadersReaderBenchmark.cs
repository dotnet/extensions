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

    [Params(false, true)]
    public bool LogContentHeaders { get; set; }

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

        var options = new LoggingOptions { LogContentHeaders = LogContentHeaders };
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
    public void HeadersReader()
    {
        _headersReader.ReadRequestHeaders(Request!, _outputBuffer);
    }
}

/*
 
 These results show comparison between a plain logic (when we enumerate over "headersToLog" dictionary),
 and an updated logic when we choose the strategy based on the number of headers to read and the number of headers to log.

BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22631.2861), VM=Hyper-V
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=8.0.100
  [Host] : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2

Job=MediumRun  Jit=RyuJit  Platform=X64
Runtime=.NET 8.0  Server=True  Toolchain=InProcessEmitToolchain
IterationCount=15  LaunchCount=2  WarmupCount=10

|           Method | HeadersCount | HeadersToLogCount |         Mean |      Error |     StdDev |   Gen0 | Allocated |
|----------------- |------------- |------------------ |-------------:|-----------:|-----------:|-------:|----------:|
| HeadersReaderNew |            0 |                 1 |     4.372 ns |  0.0120 ns |  0.0175 ns |      - |         - |
| HeadersReaderNew |            0 |                 5 |     4.459 ns |  0.0639 ns |  0.0916 ns |      - |         - |
| HeadersReaderNew |            0 |                 3 |     4.648 ns |  0.2073 ns |  0.2972 ns |      - |         - |
| HeadersReaderNew |            0 |                10 |     4.995 ns |  0.4255 ns |  0.6369 ns |      - |         - |
| HeadersReaderOld |            0 |                 1 |    18.607 ns |  0.0492 ns |  0.0689 ns |      - |         - |
| HeadersReaderOld |            0 |                 3 |    41.269 ns |  0.0759 ns |  0.1088 ns |      - |         - |
| HeadersReaderOld |            0 |                 5 |    63.525 ns |  0.1879 ns |  0.2572 ns |      - |         - |
| HeadersReaderOld |            0 |                10 |   119.163 ns |  0.6748 ns |  0.9678 ns |      - |         - |
| HeadersReaderOld |           15 |                 1 |   204.099 ns |  1.3834 ns |  1.8936 ns | 0.0100 |     256 B |
| HeadersReaderOld |            5 |                 1 |   205.044 ns |  1.7938 ns |  2.4553 ns | 0.0095 |     256 B |
| HeadersReaderNew |           15 |                 1 |   216.454 ns |  1.1861 ns |  1.6236 ns | 0.0100 |     256 B |
| HeadersReaderNew |            5 |                 1 |   217.499 ns |  1.5149 ns |  2.0736 ns | 0.0100 |     256 B |
| HeadersReaderOld |            5 |                 3 |   630.982 ns |  4.4363 ns |  5.9224 ns | 0.0305 |     768 B |
| HeadersReaderOld |           15 |                 3 |   641.780 ns |  4.4820 ns |  6.1351 ns | 0.0305 |     768 B |
| HeadersReaderNew |           15 |                 3 |   669.542 ns |  3.6006 ns |  4.9285 ns | 0.0305 |     768 B |
| HeadersReaderNew |            5 |                 3 |   677.061 ns |  3.8858 ns |  5.3190 ns | 0.0305 |     768 B |
| HeadersReaderNew |            5 |                10 |   981.974 ns |  9.8502 ns | 13.4831 ns | 0.0515 |    1336 B |
| HeadersReaderOld |            5 |                 5 | 1,126.471 ns |  7.1361 ns |  9.7680 ns | 0.0496 |    1280 B |
| HeadersReaderOld |           15 |                 5 | 1,134.430 ns |  6.2873 ns |  8.3934 ns | 0.0496 |    1280 B |
| HeadersReaderNew |           15 |                 5 | 1,153.805 ns |  3.0554 ns |  4.1823 ns | 0.0496 |    1280 B |
| HeadersReaderNew |            5 |                 5 | 1,164.717 ns |  9.8121 ns | 13.4310 ns | 0.0496 |    1280 B |
| HeadersReaderOld |            5 |                10 | 1,413.431 ns |  7.9666 ns | 10.9048 ns | 0.0496 |    1280 B |
| HeadersReaderOld |           15 |                10 | 2,489.613 ns | 19.0100 ns | 26.0211 ns | 0.0992 |    2560 B |
| HeadersReaderNew |           15 |                10 | 2,507.523 ns | 10.2106 ns | 13.9763 ns | 0.0992 |    2560 B |

 */
