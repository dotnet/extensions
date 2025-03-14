﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

internal sealed class PerIncomingRequestLogBufferManager : PerRequestLogBuffer
{
    private readonly GlobalLogBuffer _globalBuffer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LogBufferingFilterRuleSelector _ruleSelector;
    private readonly IOptionsMonitor<PerIncomingRequestLogBufferingOptions> _options;

    public PerIncomingRequestLogBufferManager(
        GlobalLogBuffer globalBuffer,
        IHttpContextAccessor httpContextAccessor,
        LogBufferingFilterRuleSelector ruleSelector,
        IOptionsMonitor<PerIncomingRequestLogBufferingOptions> options)
    {
        _globalBuffer = globalBuffer;
        _httpContextAccessor = httpContextAccessor;
        _ruleSelector = ruleSelector;
        _options = options;
    }

    public override void Flush()
    {
        _httpContextAccessor.HttpContext?.RequestServices.GetService<PerIncomingRequestBufferHolder>()?.Flush();
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
        PerIncomingRequestBufferHolder? bufferHolder = httpContext.RequestServices.GetService<PerIncomingRequestBufferHolder>();
        ILoggingBuffer? buffer = bufferHolder?.GetOrAdd(category, _ =>
            new PerIncomingRequestBuffer(bufferedLogger, category, _ruleSelector, _options));

        if (buffer is null)
        {
            return _globalBuffer.TryEnqueue(bufferedLogger, logEntry);
        }

        return buffer.TryEnqueue(logEntry);
    }
}
#endif
