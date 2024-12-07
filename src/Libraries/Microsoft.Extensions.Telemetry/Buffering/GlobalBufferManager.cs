// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Buffering;
internal sealed class GlobalBufferManager : BackgroundService, IBufferManager
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

    public ILoggingBuffer CreateBuffer(IBufferSink bufferSink, string category)
        => Buffers.GetOrAdd(category, _ => new GlobalBuffer(bufferSink, _options, _timeProvider));

    [RequiresUnreferencedCode("Calls Microsoft.Extensions.Logging.ILoggingBuffer.Flush()")]
    public void Flush()
    {
        foreach (var buffer in Buffers.Values)
        {
            buffer.Flush();
        }
    }

    [RequiresUnreferencedCode("Calls Microsoft.Extensions.Logging.ILoggingBuffer.TryEnqueue<TState>(LogLevel, String, EventId, TState, Exception, Func<TState, Exception, String>)")]
    public bool TryEnqueue<TState>(
        IBufferSink bufferSink,
        LogLevel logLevel,
        string category,
        EventId eventId, TState attributes,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var buffer = CreateBuffer(bufferSink, category);
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
#endif
