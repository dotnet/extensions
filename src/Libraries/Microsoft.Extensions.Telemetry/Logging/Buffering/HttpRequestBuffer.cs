// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;
internal class HttpRequestBuffer : ILoggingBuffer
{
    private readonly HttpRequestBufferingOptions _options;
    private readonly IBufferedLogger[] _loggers;
    private readonly ConcurrentQueue<HttpRequestBufferingLogRecord> _queue;
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private DateTimeOffset _lastFlushTimestamp;

    public HttpRequestBuffer(IOptions<HttpRequestBufferingOptions> options, IEnumerable<IBufferedLogger> loggers)
    {
        _options = options.Value;
        _loggers = loggers.ToArray();
        _queue = new ConcurrentQueue<HttpRequestBufferingLogRecord>();
        _lastFlushTimestamp = _timeProvider.GetUtcNow();
    }

    public void Enqueue(LogLevel logLevel, EventId eventId, IReadOnlyList<KeyValuePair<string, object?>> joiner, Exception? exception, string v)
    {
        if (_queue.Count >= _options.Capacity)
        {
            _ = _queue.TryDequeue(out HttpRequestBufferingLogRecord? _);
        }

        var record = new HttpRequestBufferingLogRecord(logLevel, eventId, joiner, exception, v);
        _queue.Enqueue(record);
    }

    public void Flush()
    {
        var result = new List<BufferedLogRecord>();

        while (!_queue.IsEmpty)
        {
            if (_queue.TryDequeue(out HttpRequestBufferingLogRecord? item))
            {
                result.Add(item);
            }
        }

        for (int i = 0; i < _loggers.Length; i++)
        {
            _loggers[i].LogRecords(result);
        }

        _lastFlushTimestamp = _timeProvider.GetUtcNow();
    }

    public bool IsEnabled(string category, LogLevel logLevel, EventId eventId)
    {
        if (_timeProvider.GetUtcNow() > _lastFlushTimestamp + _options.SuspendAfterFlushDuration)
        {
            return false;
        }

        // TODO: check if the supplied pattern applies to any of the options.Rules:
        _ = _options.Rules;
        _ = category;
        _ = logLevel;
        _ = eventId;

        return true;
    }
}
