// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal sealed class GlobalBuffer : IDisposable
{
    private const int MaxBatchSize = 256;
    private static readonly ObjectPool<List<DeserializedLogRecord>> _recordsToEmitListPool =
        PoolFactory.CreateListPoolWithCapacity<DeserializedLogRecord>(MaxBatchSize);

    private readonly IOptionsMonitor<GlobalLogBufferingOptions> _options;
    private readonly IBufferedLogger _bufferedLogger;
    private readonly TimeProvider _timeProvider;
    private readonly LogBufferingFilterRuleSelector _ruleSelector;
    private readonly IDisposable? _optionsChangeTokenRegistration;
    private readonly string _category;
    private readonly Lock _bufferSwapLock = new();

    private ConcurrentQueue<SerializedLogRecord> _activeBuffer = new();
    private ConcurrentQueue<SerializedLogRecord> _standbyBuffer = new();

    private DateTimeOffset _lastFlushTimestamp;
    private int _activeBufferSize;
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
        if (_timeProvider.GetUtcNow() < _lastFlushTimestamp + _options.CurrentValue.AutoFlushDuration)
        {
            return false;
        }

        IReadOnlyList<KeyValuePair<string, object?>>? attributes = logEntry.State as IReadOnlyList<KeyValuePair<string, object?>>;
        if (attributes is null)
        {
            // we expect state to be either ModernTagJoiner or LegacyTagJoiner
            // which both implement IReadOnlyList<KeyValuePair<string, object?>>
            // and if not, we throw an exception
            Throw.InvalidOperationException(
                $"Unsupported type of log state detected: {typeof(TState)}, expected IReadOnlyList<KeyValuePair<string, object?>>");
        }

        if (_ruleSelector.Select(_lastKnownGoodFilterRules, logEntry.LogLevel, logEntry.EventId, attributes) is null)
        {
            // buffering is not enabled for this log entry,
            // return false to indicate that the log entry should be logged normally.
            return false;
        }

        SerializedLogRecord serializedLogRecord = SerializedLogRecordFactory.Create(
            logEntry.LogLevel,
            logEntry.EventId,
            _timeProvider.GetUtcNow(),
            attributes,
            logEntry.Exception,
            logEntry.Formatter(logEntry.State, logEntry.Exception));

        if (serializedLogRecord.SizeInBytes > _options.CurrentValue.MaxLogRecordSizeInBytes)
        {
            SerializedLogRecordFactory.Return(serializedLogRecord);
            return false;
        }

        lock (_bufferSwapLock)
        {
            _activeBuffer.Enqueue(serializedLogRecord);
            _ = Interlocked.Add(ref _activeBufferSize, serializedLogRecord.SizeInBytes);
        }

        TrimExcessRecords();

        return true;
    }

    public void Flush()
    {
        _lastFlushTimestamp = _timeProvider.GetUtcNow();

        ConcurrentQueue<SerializedLogRecord> tempBuffer;
        int numItemsToEmit;
        lock (_bufferSwapLock)
        {
            tempBuffer = _activeBuffer;
            _activeBuffer = _standbyBuffer;
            _standbyBuffer = tempBuffer;

            numItemsToEmit = tempBuffer.Count;

            _ = Interlocked.Exchange(ref _activeBufferSize, 0);
        }

        for (int offset = 0; offset < numItemsToEmit && !tempBuffer.IsEmpty; offset += MaxBatchSize)
        {
            int currentBatchSize = Math.Min(MaxBatchSize, numItemsToEmit - offset);
            List<DeserializedLogRecord> recordsToEmit = _recordsToEmitListPool.Get();
            try
            {
                for (int i = 0; i < currentBatchSize && tempBuffer.TryDequeue(out SerializedLogRecord bufferedRecord); i++)
                {
                    recordsToEmit.Add(new DeserializedLogRecord(
                        bufferedRecord.Timestamp,
                        bufferedRecord.LogLevel,
                        bufferedRecord.EventId,
                        bufferedRecord.Exception,
                        bufferedRecord.FormattedMessage,
                        bufferedRecord.Attributes));
                }

                _bufferedLogger.LogRecords(recordsToEmit);
            }
            finally
            {
                _recordsToEmitListPool.Return(recordsToEmit);
            }
        }
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

    private void TrimExcessRecords()
    {
        while (_activeBufferSize > _options.CurrentValue.MaxBufferSizeInBytes &&
               _activeBuffer.TryDequeue(out SerializedLogRecord item))
        {
            _ = Interlocked.Add(ref _activeBufferSize, -item.SizeInBytes);
            SerializedLogRecordFactory.Return(item);
        }
    }
}
#endif
