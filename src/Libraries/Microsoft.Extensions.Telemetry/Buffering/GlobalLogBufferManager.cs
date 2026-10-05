// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal sealed class GlobalLogBufferManager : GlobalLogBuffer
{
    internal readonly ConcurrentDictionary<string, GlobalBuffer> Buffers = [];
    private readonly IOptionsMonitor<GlobalLogBufferingOptions> _options;
    private readonly TimeProvider _timeProvider;

    public GlobalLogBufferManager(IOptionsMonitor<GlobalLogBufferingOptions> options)
        : this(options, TimeProvider.System)
    {
    }

    internal GlobalLogBufferManager(
        IOptionsMonitor<GlobalLogBufferingOptions> options,
        TimeProvider timeProvider)
    {
        _options = options;
        _timeProvider = timeProvider;
    }

    public override void Flush()
    {
        foreach (GlobalBuffer buffer in Buffers.Values)
        {
            buffer.Flush();
        }
    }

    public override bool TryEnqueue<TState>(IBufferedLogger bufferedLogger, in LogEntry<TState> logEntry)
    {
        string category = logEntry.Category;
#if NET
        GlobalBuffer buffer = Buffers.GetOrAdd(category, static (category, state) => new GlobalBuffer(
            state.bufferedLogger,
            category,
            state._options,
            state._timeProvider),
            (bufferedLogger, _options, _timeProvider));
#else
        GlobalBuffer buffer = Buffers.GetOrAdd(category, category => new GlobalBuffer(
            bufferedLogger,
            category,
            _options,
            _timeProvider));
#endif
        return buffer.TryEnqueue(logEntry);
    }
}
#endif
