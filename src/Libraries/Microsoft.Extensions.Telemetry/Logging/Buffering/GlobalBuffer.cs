// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

internal class GlobalBuffer : BackgroundService, ILoggingBuffer
{
    private readonly GlobalBufferingOptions _options;
    private readonly IBufferedLogger[] _loggers;
    private readonly ConcurrentQueue<GlobalBufferedLogRecord> _queue;
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private DateTimeOffset _lastFlushTimestamp;

    public GlobalBuffer(IOptions<GlobalBufferingOptions> options, IEnumerable<IBufferedLogger> loggers)
    {
        _options = options.Value;
        _loggers = loggers.ToArray();
        _queue = new ConcurrentQueue<GlobalBufferedLogRecord>();
        _lastFlushTimestamp = _timeProvider.GetUtcNow();
    }

    public void Enqueue(LogLevel logLevel, EventId eventId, IReadOnlyList<KeyValuePair<string, object?>> joiner, Exception? exception, string v)
    {
        if (_queue.Count >= _options.Capacity)
        {
            _ = _queue.TryDequeue(out GlobalBufferedLogRecord? _);
        }

        var record = new GlobalBufferedLogRecord(logLevel, eventId, joiner, exception, v);
        _queue.Enqueue(record);
    }

    public void Flush()
    {
        var result = new List<BufferedLogRecord>();

        while (!_queue.IsEmpty)
        {
            if (_queue.TryDequeue(out GlobalBufferedLogRecord? item))
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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intentionally Consume All. Allow no escapes.")]
    [SuppressMessage("Blocker Bug", "S2190:Loops and recursions should not be infinite", Justification = "Terminate when Delay throws an exception on cancellation")]
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await _timeProvider.Delay(_options.Duration, cancellationToken).ConfigureAwait(false);

            while (!_queue.IsEmpty)
            {
                if (_queue.TryPeek(out GlobalBufferedLogRecord? item))
                {
                    if (_timeProvider.GetUtcNow() - item.Timestamp > _options.Duration)
                    {
                        _ = _queue.TryDequeue(out _);
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
