// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

internal sealed class IncomingRequestLogBuffer
{
    private const int MaxBatchSize = 256;
    private static readonly ObjectPool<List<DeserializedLogRecord>> _recordsToEmitListPool =
        PoolFactory.CreateListPoolWithCapacity<DeserializedLogRecord>(MaxBatchSize);

    private readonly IBufferedLogger _bufferedLogger;
    private readonly LogBufferingFilterRuleSelector _ruleSelector;
    private readonly IOptionsMonitor<PerRequestLogBufferingOptions> _options;
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly LogBufferingFilterRule[] _filterRules;
    private readonly Lock _bufferSwapLock = new();

    private ConcurrentQueue<SerializedLogRecord> _activeBuffer = new();
    private ConcurrentQueue<SerializedLogRecord> _standbyBuffer = new();
    private int _activeBufferSize;
    private DateTimeOffset _lastFlushTimestamp;

    public IncomingRequestLogBuffer(
        IBufferedLogger bufferedLogger,
        string category,
        LogBufferingFilterRuleSelector ruleSelector,
        IOptionsMonitor<PerRequestLogBufferingOptions> options)
    {
        _bufferedLogger = bufferedLogger;
        _ruleSelector = ruleSelector;
        _options = options;
        _filterRules = LogBufferingFilterRuleSelector.SelectByCategory(_options.CurrentValue.Rules.ToArray(), category);
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

        if (_ruleSelector.Select(_filterRules, logEntry.LogLevel, logEntry.EventId, attributes) is null)
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
        }

        _ = Interlocked.Add(ref _activeBufferSize, serializedLogRecord.SizeInBytes);

        TrimExcessRecords();

        return true;
    }

    public void Flush()
    {
        _lastFlushTimestamp = _timeProvider.GetUtcNow();

        SerializedLogRecord[] bufferedRecords;
        lock (_bufferSwapLock)
        {
            bufferedRecords = _activeBuffer.ToArray();

            ConcurrentQueue<SerializedLogRecord> tempBuffer = _activeBuffer;
            _activeBuffer = _standbyBuffer;
            tempBuffer.Clear();
            _standbyBuffer = tempBuffer;
            Interlocked.Exchange(ref _activeBufferSize, 0);
        }

        // Process records in batches
        for (int offset = 0; offset < bufferedRecords.Length; offset += MaxBatchSize)
        {
            int currentBatchSize = Math.Min(MaxBatchSize, bufferedRecords.Length - offset);
            List<DeserializedLogRecord>? recordsToEmit = null;
            try
            {
                recordsToEmit = _recordsToEmitListPool.Get();

                for (int i = 0; i < currentBatchSize; i++)
                {
                    SerializedLogRecord bufferedRecord = bufferedRecords[offset + i];
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
                if (recordsToEmit is not null)
                {
                    _recordsToEmitListPool.Return(recordsToEmit);
                }
            }
        }

        SerializedLogRecordFactory.Return(bufferedRecords);
    }

    private void TrimExcessRecords()
    {
        while (_activeBufferSize > _options.CurrentValue.MaxPerRequestBufferSizeInBytes &&
               _activeBuffer.TryDequeue(out SerializedLogRecord item))
        {
            _ = Interlocked.Add(ref _activeBufferSize, -item.SizeInBytes);
            SerializedLogRecordFactory.Return(item);
        }
    }
}
#endif
