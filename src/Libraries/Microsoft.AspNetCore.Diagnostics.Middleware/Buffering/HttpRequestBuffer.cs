// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

internal sealed class HttpRequestBuffer : ILoggingBuffer
{
    private readonly IOptionsMonitor<HttpRequestBufferOptions> _options;
    private readonly ConcurrentDictionary<IBufferedLogger, ConcurrentQueue<HttpRequestBufferedLogRecord>> _buffers;
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private DateTimeOffset _lastFlushTimestamp;

    public HttpRequestBuffer(IOptionsMonitor<HttpRequestBufferOptions> options)
    {
        _options = options;
        _buffers = new ConcurrentDictionary<IBufferedLogger, ConcurrentQueue<HttpRequestBufferedLogRecord>>();
        _lastFlushTimestamp = _timeProvider.GetUtcNow();
    }

    internal HttpRequestBuffer(IOptionsMonitor<HttpRequestBufferOptions> options, TimeProvider timeProvider)
        : this(options)
    {
        _timeProvider = timeProvider;
        _lastFlushTimestamp = _timeProvider.GetUtcNow();
    }

    public bool TryEnqueue(
        IBufferedLogger logger,
        LogLevel logLevel,
        string category,
        EventId eventId,
        IReadOnlyList<KeyValuePair<string, object?>> joiner,
        Exception? exception,
        string formatter)
    {
        if (!IsEnabled(category, logLevel, eventId))
        {
            return false;
        }

        var record = new HttpRequestBufferedLogRecord(logLevel, eventId, joiner, exception, formatter);
        var queue = _buffers.GetOrAdd(logger, _ => new ConcurrentQueue<HttpRequestBufferedLogRecord>());

        // probably don't need to limit buffer capacity?
        // because buffer is disposed when the respective HttpContext is disposed
        // don't expect it to grow so much to cause a problem?
        if (queue.Count >= _options.CurrentValue.PerRequestCapacity)
        {
            _ = queue.TryDequeue(out HttpRequestBufferedLogRecord? _);
        }

        queue.Enqueue(record);

        return true;
    }

    public void Flush()
    {
        foreach (var (logger, queue) in _buffers)
        {
            var result = new List<BufferedLogRecord>();
            while (!queue.IsEmpty)
            {
                if (queue.TryDequeue(out HttpRequestBufferedLogRecord? item))
                {
                    result.Add(item);
                }
            }

            logger.LogRecords(result);
        }

        _lastFlushTimestamp = _timeProvider.GetUtcNow();
    }

    public bool IsEnabled(string category, LogLevel logLevel, EventId eventId)
    {
        if (_timeProvider.GetUtcNow() < _lastFlushTimestamp + _options.CurrentValue.SuspendAfterFlushDuration)
        {
            return false;
        }

        LoggerFilterRuleSelector.Select<BufferFilterRule>(_options.CurrentValue.Rules, category, logLevel, eventId, out BufferFilterRule? rule);

        return rule is not null;
    }
}
#endif
