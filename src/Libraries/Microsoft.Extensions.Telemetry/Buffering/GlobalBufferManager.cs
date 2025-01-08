// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal sealed class GlobalBufferManager : BackgroundService, IGlobalBufferManager
{
    internal readonly ConcurrentDictionary<string, ILoggingBuffer> Buffers = [];
    private readonly IOptionsMonitor<GlobalBufferOptions> _options;
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public GlobalBufferManager(IOptionsMonitor<GlobalBufferOptions> options)
    {
        _options = options;
    }

    internal GlobalBufferManager(IOptionsMonitor<GlobalBufferOptions> options, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _options = options;
    }

    public ILoggingBuffer CreateBuffer(IBufferedLogger bufferedLogger, string category)
        => Buffers.GetOrAdd(category, _ => new GlobalBuffer(bufferedLogger, _options, _timeProvider));

    public void Flush()
    {
        foreach (var buffer in Buffers.Values)
        {
            buffer.Flush();
        }
    }

    public bool TryEnqueue<TState>(
        IBufferedLogger bufferedLogger,
        LogLevel logLevel,
        string category,
        EventId eventId, TState attributes,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var buffer = CreateBuffer(bufferedLogger, category);
        return buffer.TryEnqueue(logLevel, category, eventId, attributes, exception, formatter);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _timeProvider.Delay(_options.CurrentValue.Duration, cancellationToken).ConfigureAwait(false);
            foreach (var buffer in Buffers.Values)
            {
                buffer.TruncateOverlimit();
            }
        }
    }
}
