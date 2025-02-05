// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

internal sealed class HttpRequestBufferManager : IHttpRequestBufferManager
{
    private readonly IGlobalBufferManager _globalBufferManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptionsMonitor<HttpRequestBufferOptions> _requestOptions;
    private readonly IOptionsMonitor<GlobalBufferOptions> _globalOptions;

    public HttpRequestBufferManager(
        IGlobalBufferManager globalBufferManager,
        IHttpContextAccessor httpContextAccessor,
        IOptionsMonitor<HttpRequestBufferOptions> requestOptions,
        IOptionsMonitor<GlobalBufferOptions> globalOptions)
    {
        _globalBufferManager = globalBufferManager;
        _httpContextAccessor = httpContextAccessor;
        _requestOptions = requestOptions;
        _globalOptions = globalOptions;
    }

    public void FlushNonRequestLogs() => _globalBufferManager.Flush();

    public void FlushCurrentRequestLogs()
    {
        _httpContextAccessor.HttpContext?.RequestServices.GetService<HttpRequestBufferHolder>()?.Flush();
    }

    public bool TryEnqueue<TState>(
        IBufferedLogger bufferedLogger,
        LogLevel logLevel,
        string category,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return _globalBufferManager.TryEnqueue(bufferedLogger, logLevel, category, eventId, state, exception, formatter);
        }

        HttpRequestBufferHolder? bufferHolder = httpContext.RequestServices.GetService<HttpRequestBufferHolder>();
        ILoggingBuffer? buffer = bufferHolder?.GetOrAdd(category, _ => new HttpRequestBuffer(bufferedLogger, _requestOptions, _globalOptions)!);

        if (buffer is null)
        {
            return _globalBufferManager.TryEnqueue(bufferedLogger, logLevel, category, eventId, state, exception, formatter);
        }

        return buffer.TryEnqueue(logLevel, category, eventId, state, exception, formatter);
    }
}
