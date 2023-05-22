// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Bench.Benchmarks;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Bench")]
public class HugeHttpClientLoggingBenchmark
{
    private const string DataFileName = "HugeBody.txt";
    private const int ReadSizeLimit = 53248;
    private static HttpRequestMessage Request => new(HttpMethod.Post, "https://www.microsoft.com");

    private static readonly System.Net.Http.HttpClient _hugeNoLog
        = HttpClientFactory.CreateWithoutLogging(DataFileName);

    private static readonly System.Net.Http.HttpClient _hugeLogAll
        = HttpClientFactory.CreateWithLoggingLogAll(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _hugeLogRequest
        = HttpClientFactory.CreateWithLoggingLogRequest(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _hugeLogResponse
        = HttpClientFactory.CreateWithLoggingLogResponse(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _hugeNoLogChunked
        = HttpClientFactory.CreateWithoutLogging_ChunkedEncoding(DataFileName);

    private static readonly System.Net.Http.HttpClient _hugeLogAllChunked
        = HttpClientFactory.CreateWithLoggingLogAll_ChunkedEncoding(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _hugeLogRequestChunked
        = HttpClientFactory.CreateWithLoggingLogRequest_ChunkedEncoding(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _hugeLogResponseChunked
        = HttpClientFactory.CreateWithLoggingLogResponse_ChunkedEncoding(DataFileName, ReadSizeLimit);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Huge_No_Log_HeadersRead()
    {
        var response = await _hugeNoLog.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Huge_No_Log_ContentRead()
    {
        var response = await _hugeNoLog.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Huge_Log_All_HeadersRead()
    {
        var response = await _hugeLogAll.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Huge_Log_All_ContentRead()
    {
        var response = await _hugeLogAll.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Huge_Log_Request_HeadersRead()
    {
        var response = await _hugeLogRequest.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Huge_Log_Request_ContentRead()
    {
        var response = await _hugeLogRequest.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Huge_Log_Response_HeadersRead()
    {
        var response = await _hugeLogResponse.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Huge_Log_Response_ContentRead()
    {
        var response = await _hugeLogResponse.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Huge_No_Log_HeadersRead_ChunkedEncoding()
    {
        var response = await _hugeNoLogChunked.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Huge_No_Log_ContentRead_ChunkedEncoding()
    {
        var response = await _hugeNoLogChunked.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Huge_Log_All_HeadersRead_ChunkedEncoding()
    {
        var response = await _hugeLogAllChunked.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Huge_Log_All_ContentRead_ChunkedEncoding()
    {
        var response = await _hugeLogAllChunked.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Huge_Log_Request_HeadersRead_ChunkedEncoding()
    {
        var response = await _hugeLogRequestChunked.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Huge_Log_Request_ContentRead_ChunkedEncoding()
    {
        var response = await _hugeLogRequestChunked.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Huge_Log_Response_HeadersRead_ChunkedEncoding()
    {
        var response = await _hugeLogResponseChunked.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Huge_Log_Response_ContentRead_ChunkedEncoding()
    {
        var response = await _hugeLogResponseChunked.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }
}
