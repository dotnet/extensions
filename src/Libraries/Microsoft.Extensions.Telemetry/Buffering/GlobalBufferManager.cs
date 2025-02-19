// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal sealed class GlobalBufferManager : LogBuffer
{
    internal readonly ConcurrentDictionary<string, ILoggingBuffer> Buffers = [];
    private readonly IOptionsMonitor<GlobalLogBufferingOptions> _options;
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public GlobalBufferManager(IOptionsMonitor<GlobalLogBufferingOptions> options)
    {
        _options = options;
    }

    internal GlobalBufferManager(IOptionsMonitor<GlobalLogBufferingOptions> options, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _options = options;
    }

    public override void Flush()
    {
        foreach (var buffer in Buffers.Values)
        {
            buffer.Flush();
        }
    }

    public override bool TryEnqueue<TState>(IBufferedLogger bufferedLogger, in LogEntry<TState> logEntry)
    {
        var buffer = Buffers.GetOrAdd(logEntry.Category, _ => new GlobalBuffer(bufferedLogger, _options, _timeProvider));
        return buffer.TryEnqueue(logEntry);
    }
}
