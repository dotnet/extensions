// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

internal sealed class PerRequestLogBufferManager : PerRequestLogBuffer
{
    internal readonly IOptionsMonitor<PerRequestLogBufferingOptions> Options;

    private readonly GlobalLogBuffer _globalBuffer;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PerRequestLogBufferManager(
        GlobalLogBuffer globalBuffer,
        IHttpContextAccessor httpContextAccessor,
        IOptionsMonitor<PerRequestLogBufferingOptions> options)
    {
        _globalBuffer = globalBuffer;
        _httpContextAccessor = httpContextAccessor;
        Options = options;
    }

    public override void Flush()
    {
        IServiceProvider? requestServices = _httpContextAccessor.HttpContext?.RequestServices;
        requestServices?.GetService<IncomingRequestLogBufferHolder>()?.Flush();
        _globalBuffer.Flush();
    }

    public override bool TryEnqueue<TState>(IBufferedLogger bufferedLogger, in LogEntry<TState> logEntry)
    {
        IServiceProvider? requestServices = _httpContextAccessor.HttpContext?.RequestServices;
        if (requestServices is null)
        {
            return _globalBuffer.TryEnqueue(bufferedLogger, logEntry);
        }

        string category = logEntry.Category;
        IncomingRequestLogBufferHolder? bufferHolder =
            requestServices.GetService<IncomingRequestLogBufferHolder>();
        IncomingRequestLogBuffer? buffer = bufferHolder?.GetOrAdd(category, _ =>
            new IncomingRequestLogBuffer(bufferedLogger, category, Options));

        if (buffer is null)
        {
            return _globalBuffer.TryEnqueue(bufferedLogger, logEntry);
        }

        return buffer.TryEnqueue(logEntry);
    }
}
#endif
