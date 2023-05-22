// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Bench.Benchmarks;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Bench")]
public class MediumHttpClientLoggingBenchmark
{
    private const string DataFileName = "MediumBody.txt";
    private const int ReadSizeLimit = 16384;
    private static HttpRequestMessage Request => new(HttpMethod.Post, "https://www.microsoft.com");

    private static readonly System.Net.Http.HttpClient _mediumNoLog
        = HttpClientFactory.CreateWithoutLogging(DataFileName);

    private static readonly System.Net.Http.HttpClient _mediumLogAll
        = HttpClientFactory.CreateWithLoggingLogAll(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _mediumLogRequest
        = HttpClientFactory.CreateWithLoggingLogRequest(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _mediumLogResponse
        = HttpClientFactory.CreateWithLoggingLogResponse(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _mediumNoLogChunked
        = HttpClientFactory.CreateWithoutLogging_ChunkedEncoding(DataFileName);

    private static readonly System.Net.Http.HttpClient _mediumLogAllChunked
        = HttpClientFactory.CreateWithLoggingLogAll_ChunkedEncoding(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _mediumLogRequestChunked
        = HttpClientFactory.CreateWithLoggingLogRequest_ChunkedEncoding(DataFileName, ReadSizeLimit);

    private static readonly System.Net.Http.HttpClient _mediumLogResponseChunked
        = HttpClientFactory.CreateWithLoggingLogResponse_ChunkedEncoding(DataFileName, ReadSizeLimit);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Medium_No_Log_HeadersRead()
    {
        var response = await _mediumNoLog.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Medium_No_Log_ContentRead()
    {
        var response = await _mediumNoLog.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Medium_Log_All_HeadersRead()
    {
        var response = await _mediumLogAll.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Medium_Log_All_ContentRead()
    {
        var response = await _mediumLogAll.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Medium_Log_Request_HeadersRead()
    {
        var response = await _mediumLogRequest.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Medium_Log_Request_ContentRead()
    {
        var response = await _mediumLogRequest.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Medium_Log_Response_HeadersRead()
    {
        var response = await _mediumLogResponse.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("Seekable")]
    public async Task<HttpResponseMessage> Medium_Log_Response_ContentRead()
    {
        var response = await _mediumLogResponse.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Medium_No_Log_HeadersRead_ChunkedEncoding()
    {
        var response = await _mediumNoLogChunked.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Medium_No_Log_ContentRead_ChunkedEncoding()
    {
        var response = await _mediumNoLogChunked.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Medium_Log_All_HeadersRead_ChunkedEncoding()
    {
        var response = await _mediumLogAllChunked.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Medium_Log_All_ContentRead_ChunkedEncoding()
    {
        var response = await _mediumLogAllChunked.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Medium_Log_Request_HeadersRead_ChunkedEncoding()
    {
        var response = await _mediumLogRequestChunked.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Medium_Log_Request_ContentRead_ChunkedEncoding()
    {
        var response = await _mediumLogRequestChunked.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Medium_Log_Response_HeadersRead_ChunkedEncoding()
    {
        var response = await _mediumLogResponseChunked.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }

    [Benchmark]
    [BenchmarkCategory("NonSeekable")]
    public async Task<HttpResponseMessage> Medium_Log_Response_ContentRead_ChunkedEncoding()
    {
        var response = await _mediumLogResponseChunked.SendAsync(Request, HttpCompletionOption.ResponseContentRead, CancellationToken.None)
             .ConfigureAwait(false);

        return response;
    }
}
