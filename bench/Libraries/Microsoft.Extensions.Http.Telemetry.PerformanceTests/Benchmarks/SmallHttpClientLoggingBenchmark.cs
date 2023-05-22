// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Bench.Benchmarks;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Bench")]
public class SmallHttpClientLoggingBenchmark
{
    private const string DataFileName = "SmallBody.txt";
    private const int ReadSizeLimit = 8192;
    private static HttpRequestMessage Request => new(HttpMethod.Post, "https://www.microsoft.com");

    private static readonly System.Net.Http.HttpClient _smallNoLog
        = HttpClientFactory.CreateWithoutLogging(DataFileName);

    private static readonly System.Net.Http.HttpClient _smallLogAll
        = HttpClientFactory.CreateWithLoggingLogAll(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _smallLogRequest
        = HttpClientFactory.CreateWithLoggingLogRequest(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _smallLogResponse
        = HttpClientFactory.CreateWithLoggingLogResponse(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _smallNoLogChunked
        = HttpClientFactory.CreateWithoutLogging_ChunkedEncoding(DataFileName);

    private static readonly System.Net.Http.HttpClient _smallLogAllChunked
        = HttpClientFactory.CreateWithLoggingLogAll_ChunkedEncoding(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _smallLogRequestChunked
        = HttpClientFactory.CreateWithLoggingLogRequest_ChunkedEncoding(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _smallLogResponseChunked
        = HttpClientFactory.CreateWithLoggingLogResponse_ChunkedEncoding(DataFileName, ReadSizeLimit);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Small_No_Log_HeadersRead()
    {
        var response = await _smallNoLog.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Small_No_Log_ContentRead()
    {
        var response = await _smallNoLog.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Small_Log_All_HeadersRead()
    {
        var response = await _smallLogAll.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Small_Log_All_ContentRead()
    {
        var response = await _smallLogAll.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Small_Log_Request_HeadersRead()
    {
        var response = await _smallLogRequest.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Small_Log_Request_ContentRead()
    {
        var response = await _smallLogRequest.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Small_Log_Response_HeadersRead()
    {
        var response = await _smallLogResponse.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Small_Log_Response_ContentRead()
    {
        var response = await _smallLogResponse.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Small_No_Log_HeadersRead_ChunkedEncoding()
    {
        var response = await _smallNoLogChunked.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Small_No_Log_ContentRead_ChunkedEncoding()
    {
        var response = await _smallNoLogChunked.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Small_Log_All_HeadersRead_ChunkedEncoding()
    {
        var response = await _smallLogAllChunked.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Small_Log_All_ContentRead_ChunkedEncoding()
    {
        var response = await _smallLogAllChunked.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Small_Log_Request_HeadersRead_ChunkedEncoding()
    {
        var response = await _smallLogRequestChunked.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Small_Log_Request_ContentRead_ChunkedEncoding()
    {
        var response = await _smallLogRequestChunked.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Small_Log_Response_HeadersRead_ChunkedEncoding()
    {
        var response = await _smallLogResponseChunked.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Small_Log_Response_ContentRead_ChunkedEncoding()
    {
        var response = await _smallLogResponseChunked.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }
}
