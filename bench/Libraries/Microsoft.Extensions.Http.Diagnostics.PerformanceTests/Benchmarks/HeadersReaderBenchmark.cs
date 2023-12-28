// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Http.Logging.Internal;
using Microsoft.Extensions.Telemetry.Internal;

namespace Microsoft.Extensions.Http.Diagnostics.Bench.Benchmarks;

public class HeadersReaderBenchmark
{
    private readonly HttpHeadersReader _headersReader;
    private readonly List<KeyValuePair<string, string>> _outputBuffer = new(capacity: 1024);

    [Params(0, 5, 15)]
    public int HeadersCount { get; set; }

    [Params(0, 3, 5, 10)]
    public int HeadersToLogCount { get; set; }
    public HttpRequestMessage Request { get; }

    public HeadersReaderBenchmark()
    {
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

        var redactor = new HttpHeadersRedactor(NullRedactorProvider.Instance);
        _headersReader = new HttpHeadersReader(new StaticOptionsMonitor<LoggingOptions>(options), redactor);
    }

    [Benchmark]
    public void HeadersReader()
    {
        _headersReader.ReadRequestHeaders(Request, _outputBuffer);
    }
}
