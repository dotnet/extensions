// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Buffering;

internal sealed class GlobalLogBufferManager : GlobalLogBuffer
{
    private readonly ConcurrentDictionary<string, GlobalBuffer> _buffers = [];
    private readonly IOptionsMonitor<GlobalLogBufferingOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly LogBufferingFilterRuleSelector _ruleSelector;

    public GlobalLogBufferManager(
        LogBufferingFilterRuleSelector ruleSelector,
        IOptionsMonitor<GlobalLogBufferingOptions> options)
        : this(ruleSelector, options, TimeProvider.System)
    {
    }

    internal GlobalLogBufferManager(
        LogBufferingFilterRuleSelector ruleSelector,
        IOptionsMonitor<GlobalLogBufferingOptions> options,
        TimeProvider timeProvider)
    {
        _ruleSelector = ruleSelector;
        _options = options;
        _timeProvider = timeProvider;
    }

    public override void Flush()
    {
        foreach (GlobalBuffer buffer in _buffers.Values)
        {
            buffer.Flush();
        }
    }

    public override bool TryEnqueue<TState>(IBufferedLogger bufferedLogger, in LogEntry<TState> logEntry)
    {
        string category = logEntry.Category;
        GlobalBuffer buffer = _buffers.GetOrAdd(category, _ => new GlobalBuffer(
            bufferedLogger,
            category,
            _ruleSelector,
            _options,
            _timeProvider));
        return buffer.TryEnqueue(logEntry);
    }
}
#endif
