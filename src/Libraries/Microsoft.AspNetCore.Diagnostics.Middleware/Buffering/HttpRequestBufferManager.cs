// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

internal sealed class HttpRequestBufferManager : IHttpRequestBufferManager
{
    private readonly GlobalBufferManager _globalBufferManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptionsMonitor<HttpRequestBufferOptions> _requestOptions;
    private readonly IOptionsMonitor<GlobalBufferOptions> _globalOptions;

    public HttpRequestBufferManager(
        GlobalBufferManager globalBufferManager,
        IHttpContextAccessor httpContextAccessor,
        IOptionsMonitor<HttpRequestBufferOptions> requestOptions,
        IOptionsMonitor<GlobalBufferOptions> globalOptions)
    {
        _globalBufferManager = globalBufferManager;
        _httpContextAccessor = httpContextAccessor;
        _requestOptions = requestOptions;
        _globalOptions = globalOptions;
    }

    public ILoggingBuffer CreateBuffer(IBufferedLogger bufferedLogger, string category)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return _globalBufferManager.CreateBuffer(bufferedLogger, category);
        }

        if (!httpContext.Items.TryGetValue(category, out var buffer))
        {
            var httpRequestBuffer = new HttpRequestBuffer(bufferedLogger, _requestOptions, _globalOptions);
            httpContext.Items[category] = httpRequestBuffer;
            return httpRequestBuffer;
        }

        if (buffer is not ILoggingBuffer loggingBuffer)
        {
            throw new InvalidOperationException($"Unable to parse value of {buffer} of the {category}");
        }

        return loggingBuffer;
    }

    public void FlushNonRequestLogs() => _globalBufferManager.Flush();

    public void FlushCurrentRequestLogs()
    {
        if (_httpContextAccessor.HttpContext is not null)
        {
            foreach (var kvp in _httpContextAccessor.HttpContext!.Items)
            {
                if (kvp.Value is ILoggingBuffer buffer)
                {
                    buffer.Flush();
                }
            }
        }
    }

    public bool TryEnqueue<TState>(
        IBufferedLogger bufferedLogger,
        LogLevel logLevel,
        string category,
        EventId eventId,
        TState attributes,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var buffer = CreateBuffer(bufferedLogger, category);
        return buffer.TryEnqueue(logLevel, category, eventId, attributes, exception, formatter);
    }
}
