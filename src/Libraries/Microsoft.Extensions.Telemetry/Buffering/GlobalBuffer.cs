// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal class GlobalBuffer : BackgroundService, ILoggingBuffer
{
    private readonly GlobalBufferingOptions _options;
    private readonly ConcurrentDictionary<IBufferedLogger, ConcurrentQueue<GlobalBufferedLogRecord>> _buffers;
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private DateTimeOffset _lastFlushTimestamp;

    public GlobalBuffer(IOptions<GlobalBufferingOptions> options)
    {
        _options = options.Value;
        _lastFlushTimestamp = _timeProvider.GetUtcNow();
        _buffers = new ConcurrentDictionary<IBufferedLogger, ConcurrentQueue<GlobalBufferedLogRecord>>();
    }

    public bool TryEnqueue(
        IBufferedLogger logger,
        string category,
        LogLevel logLevel,
        EventId eventId,
        IReadOnlyList<KeyValuePair<string, object?>> joiner,
        Exception? exception, string formatter)
    {
        if (!IsEnabled(category, logLevel, eventId))
        {
            return false;
        }

        var record = new GlobalBufferedLogRecord(logLevel, eventId, joiner, exception, formatter);
        var queue = _buffers.GetOrAdd(logger, _ => new ConcurrentQueue<GlobalBufferedLogRecord>());
        if (queue.Count >= _options.Capacity)
        {
            _ = queue.TryDequeue(out GlobalBufferedLogRecord? _);
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
                if (queue.TryDequeue(out GlobalBufferedLogRecord? item))
                {
                    result.Add(item);
                }
            }

            logger.LogRecords(result);

        }

        _lastFlushTimestamp = _timeProvider.GetUtcNow();
    }

    private bool IsEnabled(string category, LogLevel logLevel, EventId eventId)
    {
        if (_timeProvider.GetUtcNow() > _lastFlushTimestamp + _options.SuspendAfterFlushDuration)
        {
            return false;
        }

        return _options.Filter(category, eventId, logLevel);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intentionally Consume All. Allow no escapes.")]
    [SuppressMessage("Blocker Bug", "S2190:Loops and recursions should not be infinite", Justification = "Terminate when Delay throws an exception on cancellation")]
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await _timeProvider.Delay(_options.Duration, cancellationToken).ConfigureAwait(false);
            foreach (var (logger, queue) in _buffers)
            {
                while (!queue.IsEmpty)
                {
                    if (queue.TryPeek(out GlobalBufferedLogRecord? item))
                    {
                        if (_timeProvider.GetUtcNow() - item.Timestamp > _options.Duration)
                        {
                            _ = queue.TryDequeue(out _);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
