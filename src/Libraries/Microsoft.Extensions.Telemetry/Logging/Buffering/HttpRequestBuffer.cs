// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

internal class HttpRequestBuffer : ILoggingBuffer
{
    private readonly HttpRequestBufferingOptions _options;
    private readonly ConcurrentDictionary<IBufferedLogger, ConcurrentQueue<HttpRequestBufferedLogRecord>> _buffers;
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private DateTimeOffset _lastFlushTimestamp;

    public HttpRequestBuffer(IOptions<HttpRequestBufferingOptions> options)
    {
        _options = options.Value;
        _buffers = new ConcurrentDictionary<IBufferedLogger, ConcurrentQueue<HttpRequestBufferedLogRecord>>();
        _lastFlushTimestamp = _timeProvider.GetUtcNow();
    }

    public bool TryEnqueue(
        IBufferedLogger logger,
        string category,
        LogLevel logLevel,
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
        // becase buffer is disposed when the respective HttpContext is disposed
        // don't expect it to grow so much to cause a problem?

        // having said that, I question the usefullness of the HTTP buffering.
        // If I have 1000 RPS each with a buffer which is auto-disposed,
        // then something bad happens and 900 requests out of 1000 failed,
        // their HttpContext were disposed, as well as buffers,
        // so at this point logs are lost and it is too late to call the Flusth() method

        queue.Enqueue(record);

        return true;
    }

    public void Flush()
    {
        _lastFlushTimestamp = _timeProvider.GetUtcNow();

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
        if (_timeProvider.GetUtcNow() > _lastFlushTimestamp + _options.SuspendAfterFlushDuration)
        {
            return false;
        }

        return _options.Filter(category, eventId, logLevel);
    }
}
