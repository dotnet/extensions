// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using static Microsoft.Extensions.Logging.ExtendedLogger;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal sealed class GlobalBuffer : IDisposable
{
    private readonly IOptionsMonitor<GlobalLogBufferingOptions> _options;
    private readonly ConcurrentQueue<SerializedLogRecord> _buffer;
    private readonly IBufferedLogger _bufferedLogger;
    private readonly TimeProvider _timeProvider;
    private readonly LogBufferingFilterRuleSelector _ruleSelector;
    private readonly IDisposable? _optionsChangeTokenRegistration;
    private readonly string _category;

    private DateTimeOffset _lastFlushTimestamp;
    private int _bufferSize;
    private LogBufferingFilterRule[] _lastKnownGoodFilterRules;

    private volatile bool _disposed;

    public GlobalBuffer(
        IBufferedLogger bufferedLogger,
        string category,
        LogBufferingFilterRuleSelector ruleSelector,
        IOptionsMonitor<GlobalLogBufferingOptions> options,
        TimeProvider timeProvider)
    {
        _options = Throw.IfNull(options);
        _timeProvider = timeProvider;
        _buffer = new ConcurrentQueue<SerializedLogRecord>();
        _bufferedLogger = bufferedLogger;
        _category = Throw.IfNullOrEmpty(category);
        _ruleSelector = Throw.IfNull(ruleSelector);
        _lastKnownGoodFilterRules = LogBufferingFilterRuleSelector.SelectByCategory(_options.CurrentValue.Rules.ToArray(), _category);
        _optionsChangeTokenRegistration = options.OnChange(OnOptionsChanged);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _optionsChangeTokenRegistration?.Dispose();
    }

    public bool TryEnqueue<TState>(LogEntry<TState> logEntry)
    {
        SerializedLogRecord serializedLogRecord = default;
        if (logEntry.State is ModernTagJoiner modernTagJoiner)
        {
            if (!IsEnabled(logEntry.LogLevel, logEntry.EventId, modernTagJoiner))
            {
                return false;
            }

            serializedLogRecord = new SerializedLogRecord(logEntry.LogLevel, logEntry.EventId, _timeProvider.GetUtcNow(), modernTagJoiner, logEntry.Exception,
                ((Func<ModernTagJoiner, Exception?, string>)(object)logEntry.Formatter)(modernTagJoiner, logEntry.Exception));
        }
        else if (logEntry.State is LegacyTagJoiner legacyTagJoiner)
        {
            if (!IsEnabled(logEntry.LogLevel, logEntry.EventId, legacyTagJoiner))
            {
                return false;
            }

            serializedLogRecord = new SerializedLogRecord(logEntry.LogLevel, logEntry.EventId, _timeProvider.GetUtcNow(), legacyTagJoiner, logEntry.Exception,
                ((Func<LegacyTagJoiner, Exception?, string>)(object)logEntry.Formatter)(legacyTagJoiner, logEntry.Exception));
        }
        else
        {
            Throw.InvalidOperationException($"Unsupported type of the log state detected: {typeof(TState)}");
        }

        if (serializedLogRecord.SizeInBytes > _options.CurrentValue.MaxLogRecordSizeInBytes)
        {
            return false;
        }

        _buffer.Enqueue(serializedLogRecord);
        _ = Interlocked.Add(ref _bufferSize, serializedLogRecord.SizeInBytes);

        TrimExcessRecords();

        return true;
    }

    public void Flush()
    {
        _lastFlushTimestamp = _timeProvider.GetUtcNow();

        SerializedLogRecord[] bufferedRecords = _buffer.ToArray();
        
        // Clear() and Interlocked.Exchange operations are atomic on their own.
        // But together they are not atomic, therefore have to take a lock.
        // This is needed for an edge case when AutoFlushDuration is close to 0, e.g. buffering hardly pauses
        // and new items get buffered immediately after the _buffer.Clear() call.
        lock (_buffer)
        {
            _buffer.Clear();
            Interlocked.Exchange(ref _bufferSize, 0);
        }

        var deserializedLogRecords = new List<DeserializedLogRecord>(bufferedRecords.Length);
        foreach (SerializedLogRecord bufferedRecord in bufferedRecords)
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

    private void OnOptionsChanged(GlobalLogBufferingOptions? updatedOptions)
    {
        if (updatedOptions is null)
        {
            _lastKnownGoodFilterRules = [];
        }
        else
        {
            _lastKnownGoodFilterRules = LogBufferingFilterRuleSelector.SelectByCategory(updatedOptions.Rules.ToArray(), _category);
        }

        _ruleSelector.InvalidateCache();
    }
    
    private bool IsEnabled(LogLevel logLevel, EventId eventId, IReadOnlyList<KeyValuePair<string, object?>> attributes)
    {
        if (_timeProvider.GetUtcNow() < _lastFlushTimestamp + _options.CurrentValue.AutoFlushDuration)
        {
            return false;
        }

        return _ruleSelector.Select(_lastKnownGoodFilterRules, logLevel, eventId, attributes) is not null;
    }

    private void TrimExcessRecords()
    {
        while (_bufferSize > _options.CurrentValue.MaxBufferSizeInBytes &&
               _buffer.TryDequeue(out SerializedLogRecord item))
        {
            _ = Interlocked.Add(ref _bufferSize, -item.SizeInBytes);
        }
    }
}
#endif
