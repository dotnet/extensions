// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using static Microsoft.Extensions.Logging.ExtendedLogger;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal sealed class GlobalBuffer : ILoggingBuffer
{
    private readonly IOptionsMonitor<GlobalBufferOptions> _options;
    private readonly ConcurrentQueue<SerializedLogRecord> _buffer;
    private readonly IBufferedLogger _bufferedLogger;
    private readonly TimeProvider _timeProvider;
    private DateTimeOffset _lastFlushTimestamp;

    private int _bufferSize;
#if NETFRAMEWORK
    private object _netfxBufferLocker = new();
#endif

    public GlobalBuffer(IBufferedLogger bufferedLogger, IOptionsMonitor<GlobalBufferOptions> options, TimeProvider timeProvider)
    {
        _options = options;
        _timeProvider = timeProvider;
        _buffer = new ConcurrentQueue<SerializedLogRecord>();
        _bufferedLogger = bufferedLogger;
    }

    public bool TryEnqueue<T>(
        LogLevel logLevel,
        string category,
        EventId eventId,
        T attributes,
        Exception? exception,
        Func<T, Exception?, string> formatter)
    {
        SerializedLogRecord serializedLogRecord = default;
        if (attributes is ModernTagJoiner modernTagJoiner)
        {
            if (!IsEnabled(category, logLevel, eventId, modernTagJoiner))
            {
                return false;
            }

            serializedLogRecord = new SerializedLogRecord(logLevel, eventId, _timeProvider.GetUtcNow(), modernTagJoiner, exception,
                ((Func<ModernTagJoiner, Exception?, string>)(object)formatter)(modernTagJoiner, exception));
        }
        else if (attributes is LegacyTagJoiner legacyTagJoiner)
        {
            if (!IsEnabled(category, logLevel, eventId, legacyTagJoiner))
            {
                return false;
            }

            serializedLogRecord = new SerializedLogRecord(logLevel, eventId, _timeProvider.GetUtcNow(), legacyTagJoiner, exception,
                ((Func<LegacyTagJoiner, Exception?, string>)(object)formatter)(legacyTagJoiner, exception));
        }
        else
        {
            Throw.ArgumentException(nameof(attributes), $"Unsupported type of the log attributes object detected: {typeof(T)}");
        }

        if (serializedLogRecord.SizeInBytes > _options.CurrentValue.LogRecordSizeInBytes)
        {
            return false;
        }

        _buffer.Enqueue(serializedLogRecord);
        _ = Interlocked.Add(ref _bufferSize, serializedLogRecord.SizeInBytes);

        Trim();

        return true;
    }

    public void Flush()
    {
        _lastFlushTimestamp = _timeProvider.GetUtcNow();

        SerializedLogRecord[] bufferedRecords = _buffer.ToArray();

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

        var deserializedLogRecords = new List<DeserializedLogRecord>(bufferedRecords.Length);
        foreach (var bufferedRecord in bufferedRecords)
        {
            deserializedLogRecords.Add(
                new DeserializedLogRecord(
                    bufferedRecord.Timestamp,
                    bufferedRecord.LogLevel,
                    bufferedRecord.EventId,
                    bufferedRecord.Exception,
                    bufferedRecord.FormattedMessage,
                    bufferedRecord.Attributes));
        }

        _bufferedLogger.LogRecords(deserializedLogRecords);
    }

    private void Trim()
    {
        while (_bufferSize > _options.CurrentValue.BufferSizeInBytes && _buffer.TryDequeue(out var item))
        {
            _ = Interlocked.Add(ref _bufferSize, -item.SizeInBytes);
        }
    }

    private bool IsEnabled(string category, LogLevel logLevel, EventId eventId, IReadOnlyList<KeyValuePair<string, object?>> attributes)
    {
        if (_timeProvider.GetUtcNow() < _lastFlushTimestamp + _options.CurrentValue.SuspendAfterFlushDuration)
        {
            return false;
        }

        LoggerFilterRuleSelector.Select(_options.CurrentValue.Rules, category, logLevel, eventId, out BufferFilterRule? rule);

        return rule is not null && rule.Filter(category, logLevel, eventId, attributes);
    }
}
