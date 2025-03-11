﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

internal sealed class HttpRequestBufferManager : PerRequestLogBuffer
{
    private readonly GlobalLogBuffer _globalBuffer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptionsMonitor<HttpRequestLogBufferingOptions> _requestOptions;
    private readonly IOptionsMonitor<GlobalLogBufferingOptions> _globalOptions;
    private readonly LogBufferingFilterRuleSelector _ruleSelector;

    public HttpRequestBufferManager(
        GlobalLogBuffer globalBuffer,
        IHttpContextAccessor httpContextAccessor,
        LogBufferingFilterRuleSelector ruleSelector,
        IOptionsMonitor<HttpRequestLogBufferingOptions> requestOptions,
        IOptionsMonitor<GlobalLogBufferingOptions> globalOptions)
    {
        _globalBuffer = globalBuffer;
        _httpContextAccessor = httpContextAccessor;
        _ruleSelector = ruleSelector;
        _requestOptions = requestOptions;
        _globalOptions = globalOptions;
    }

    public override void Flush()
    {
        _httpContextAccessor.HttpContext?.RequestServices.GetService<HttpRequestBufferHolder>()?.Flush();
        _globalBuffer.Flush();
    }

    public override bool TryEnqueue<TState>(IBufferedLogger bufferedLogger, in LogEntry<TState> logEntry)
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return _globalBuffer.TryEnqueue(bufferedLogger, logEntry);
        }

        string category = logEntry.Category;
        HttpRequestBufferHolder? bufferHolder = httpContext.RequestServices.GetService<HttpRequestBufferHolder>();
        ILoggingBuffer? buffer = bufferHolder?.GetOrAdd(category, _ =>
            new HttpRequestBuffer(bufferedLogger, category, _ruleSelector, _requestOptions, _globalOptions)!);

        if (buffer is null)
        {
            return _globalBuffer.TryEnqueue(bufferedLogger, logEntry);
        }

        return buffer.TryEnqueue(logEntry);
    }
}
#endif
