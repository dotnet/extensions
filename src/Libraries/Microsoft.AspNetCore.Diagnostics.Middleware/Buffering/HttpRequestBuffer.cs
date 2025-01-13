// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using static Microsoft.Extensions.Logging.ExtendedLogger;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

internal sealed class HttpRequestBuffer : ILoggingBuffer
{
    private readonly IOptionsMonitor<HttpRequestBufferOptions> _options;
    private readonly IOptionsMonitor<GlobalBufferOptions> _globalOptions;
    private readonly ConcurrentQueue<SerializedLogRecord> _buffer;
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly IBufferedLogger _bufferedLogger;

    private DateTimeOffset _lastFlushTimestamp;
    private int _bufferSize;

    public HttpRequestBuffer(IBufferedLogger bufferedLogger,
        IOptionsMonitor<HttpRequestBufferOptions> options,
        IOptionsMonitor<GlobalBufferOptions> globalOptions)
    {
        _options = options;
        _globalOptions = globalOptions;
        _bufferedLogger = bufferedLogger;
        _buffer = new ConcurrentQueue<SerializedLogRecord>();
    }

    public bool TryEnqueue<TState>(
        LogLevel logLevel,
        string category,
        EventId eventId,
        TState attributes,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(category, logLevel, eventId))
        {
            return false;
        }

        SerializedLogRecord serializedLogRecord = default;
        if (attributes is ModernTagJoiner modernTagJoiner)
        {
            serializedLogRecord = new SerializedLogRecord(logLevel, eventId, _timeProvider.GetUtcNow(), modernTagJoiner, exception,
                ((Func<ModernTagJoiner, Exception?, string>)(object)formatter)(modernTagJoiner, exception));
        }
        else if (attributes is LegacyTagJoiner legacyTagJoiner)
        {
            serializedLogRecord = new SerializedLogRecord(logLevel, eventId, _timeProvider.GetUtcNow(), legacyTagJoiner, exception,
                ((Func<LegacyTagJoiner, Exception?, string>)(object)formatter)(legacyTagJoiner, exception));
        }
        else
        {
            Throw.ArgumentException(nameof(attributes), $"Unsupported type of the log attributes object detected: {typeof(TState)}");
        }

        if (serializedLogRecord.SizeInBytes > _globalOptions.CurrentValue.LogRecordSizeInBytes)
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

        _buffer.Clear();

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

    public bool IsEnabled(string category, LogLevel logLevel, EventId eventId)
    {
        if (_timeProvider.GetUtcNow() < _lastFlushTimestamp + _globalOptions.CurrentValue.SuspendAfterFlushDuration)
        {
            return false;
        }

        LoggerFilterRuleSelector.Select(_options.CurrentValue.Rules, category, logLevel, eventId, out BufferFilterRule? rule);

        return rule is not null;
    }

    private void Trim()
    {
        while (_bufferSize > _options.CurrentValue.PerRequestBufferSizeInBytes && _buffer.TryDequeue(out var item))
        {
            _ = Interlocked.Add(ref _bufferSize, -item.SizeInBytes);
        }
    }
}
