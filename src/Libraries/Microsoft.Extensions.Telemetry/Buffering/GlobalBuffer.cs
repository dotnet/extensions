// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using static Microsoft.Extensions.Logging.ExtendedLogger;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal sealed class GlobalBuffer : ILoggingBuffer
{
    private readonly IOptionsMonitor<GlobalBufferOptions> _options;
    private readonly ConcurrentQueue<SerializedLogRecord> _buffer;
    private readonly IBufferSink _bufferSink;
    private readonly TimeProvider _timeProvider;
    private DateTimeOffset _lastFlushTimestamp;
#if NETFRAMEWORK
    private object _netfxBufferLocker = new();
#endif

    public GlobalBuffer(IBufferSink bufferSink, IOptionsMonitor<GlobalBufferOptions> options, TimeProvider timeProvider)
    {
        _options = options;
        _timeProvider = timeProvider;
        _buffer = new ConcurrentQueue<SerializedLogRecord>();
        _bufferSink = bufferSink;
    }

    public bool TryEnqueue<T>(
        LogLevel logLevel,
        string category,
        EventId eventId,
        T attributes,
        Exception? exception,
        Func<T, Exception?, string> formatter)
    {
        if (!IsEnabled(category, logLevel, eventId))
        {
            return false;
        }

        switch (attributes)
        {
            case ModernTagJoiner modernTagJoiner:
                _buffer.Enqueue(new SerializedLogRecord(logLevel, eventId, _timeProvider.GetUtcNow(), modernTagJoiner, exception,
                    ((Func<ModernTagJoiner, Exception?, string>)(object)formatter)(modernTagJoiner, exception)));
                break;
            case LegacyTagJoiner legacyTagJoiner:
                _buffer.Enqueue(new SerializedLogRecord(logLevel, eventId, _timeProvider.GetUtcNow(), legacyTagJoiner, exception,
                    ((Func<LegacyTagJoiner, Exception?, string>)(object)formatter)(legacyTagJoiner, exception)));
                break;
            default:
                Throw.ArgumentException(nameof(attributes), $"Unsupported type of the log attributes object detected: {typeof(T)}");
                break;
        }

        return true;
    }

    public void Flush()
    {
        var result = _buffer.ToArray();

#if NETFRAMEWORK
        lock (_netfxBufferLocker)
        {
            while (_buffer.TryDequeue(out _))
            {
                // Clear the buffer
            }
        }
#else
        _buffer.Clear();
#endif

        _lastFlushTimestamp = _timeProvider.GetUtcNow();

        _bufferSink.LogRecords(result);
    }

    public void TruncateOverlimit()
    {
        // Capacity is a soft limit, which might be exceeded, esp. in multi-threaded environments.
        while (_buffer.Count > _options.CurrentValue.Capacity)
        {
            _ = _buffer.TryDequeue(out _);
        }
    }

    private bool IsEnabled(string category, LogLevel logLevel, EventId eventId)
    {
        if (_timeProvider.GetUtcNow() < _lastFlushTimestamp + _options.CurrentValue.SuspendAfterFlushDuration)
        {
            return false;
        }

        LoggerFilterRuleSelector.Select(_options.CurrentValue.Rules, category, logLevel, eventId, out BufferFilterRule? rule);

        return rule is not null;
    }
}
