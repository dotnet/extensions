// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

internal sealed class HttpRequestBufferManager : HttpRequestLogBuffer
{
    private readonly LogBuffer _globalBuffer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptionsMonitor<HttpRequestLogBufferingOptions> _requestOptions;
    private readonly IOptionsMonitor<GlobalLogBufferingOptions> _globalOptions;

    public HttpRequestBufferManager(
        LogBuffer globalBuffer,
        IHttpContextAccessor httpContextAccessor,
        IOptionsMonitor<HttpRequestLogBufferingOptions> requestOptions,
        IOptionsMonitor<GlobalLogBufferingOptions> globalOptions)
    {
        _globalBuffer = globalBuffer;
        _httpContextAccessor = httpContextAccessor;
        _requestOptions = requestOptions;
        _globalOptions = globalOptions;
    }

    public override void Flush() => _globalBuffer.Flush();

    public override void FlushCurrentRequestLogs()
    {
        _httpContextAccessor.HttpContext?.RequestServices.GetService<HttpRequestBufferHolder>()?.Flush();
    }

    public override bool TryEnqueue<TState>(IBufferedLogger bufferedLogger, in LogEntry<TState> logEntry)
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return _globalBuffer.TryEnqueue(bufferedLogger, logEntry);
        }

        HttpRequestBufferHolder? bufferHolder = httpContext.RequestServices.GetService<HttpRequestBufferHolder>();
        ILoggingBuffer? buffer = bufferHolder?.GetOrAdd(logEntry.Category, _ => new HttpRequestBuffer(bufferedLogger, _requestOptions, _globalOptions)!);

        if (buffer is null)
        {
            return _globalBuffer.TryEnqueue(bufferedLogger, logEntry);
        }

        return buffer.TryEnqueue(logEntry);
    }
}
