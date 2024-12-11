// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging;

internal sealed class GlobalBuffer : BackgroundService, ILoggingBuffer
{
    private readonly IOptionsMonitor<GlobalBufferOptions> _options;
    private readonly ConcurrentDictionary<IBufferedLogger, ConcurrentQueue<GlobalBufferedLogRecord>> _buffers;
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private DateTimeOffset _lastFlushTimestamp;

    public GlobalBuffer(IOptionsMonitor<GlobalBufferOptions> options)
    {
        _options = options;
        _lastFlushTimestamp = _timeProvider.GetUtcNow();
        _buffers = new ConcurrentDictionary<IBufferedLogger, ConcurrentQueue<GlobalBufferedLogRecord>>();
    }

    internal GlobalBuffer(IOptionsMonitor<GlobalBufferOptions> options, TimeProvider timeProvider)
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
        Exception? exception, string formatter)
    {
        if (!IsEnabled(category, logLevel, eventId))
        {
            return false;
        }

        var record = new GlobalBufferedLogRecord(logLevel, eventId, joiner, exception, formatter);
        var queue = _buffers.GetOrAdd(logger, _ => new ConcurrentQueue<GlobalBufferedLogRecord>());
        if (queue.Count >= _options.CurrentValue.Capacity)
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

    internal void RemoveExpiredItems()
    {
        foreach (var (logger, queue) in _buffers)
        {
            while (!queue.IsEmpty)
            {
                if (queue.TryPeek(out GlobalBufferedLogRecord? item))
                {
                    if (_timeProvider.GetUtcNow() - item.Timestamp > _options.CurrentValue.Duration)
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

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _timeProvider.Delay(_options.CurrentValue.Duration, cancellationToken).ConfigureAwait(false);
            RemoveExpiredItems();
        }
    }

    private bool IsEnabled(string category, LogLevel logLevel, EventId eventId)
    {
        if (_timeProvider.GetUtcNow() < _lastFlushTimestamp + _options.CurrentValue.SuspendAfterFlushDuration)
        {
            return false;
        }

        LoggerFilterRuleSelector.Select(_options.CurrentValue.Rules, category, logLevel, eventId, out BufferFilterRule? rule);

        return rule is not null;
    }
}
#endif
