// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Sampling;

#pragma warning disable CA5394 // Do not use insecure randomness

/// <summary>
/// Samples logs according to the specified probability.
/// </summary>
internal sealed class ProbabilisticSampler : LoggingSampler
{
#if NETFRAMEWORK
    private static readonly System.Threading.ThreadLocal<Random> _randomInstance = new(() => new Random());
#endif

    private readonly IOptionsMonitor<ProbabilisticSamplerOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProbabilisticSampler"/> class.
    /// </summary>
    public ProbabilisticSampler(IOptionsMonitor<ProbabilisticSamplerOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public override bool ShouldSample<TState>(in LogEntry<TState> logEntry)
    {
        if (!TryApply(logEntry, out var probability))
        {
            return true;
        }

#if NETFRAMEWORK
        return _randomInstance.Value!.Next(int.MaxValue) < int.MaxValue * probability;
#else
        return Random.Shared.Next(int.MaxValue) < int.MaxValue * probability;
#endif
    }

    private bool TryApply<TState>(in LogEntry<TState> logEntry, out double probability)
    {
        probability = 0.0;

        // TO DO: check if we can optimize this. It is a hot path and
        // we should be able to minimize number of rule selections on every log record.
        LogSamplingRuleSelector.Select(_options.CurrentValue.Rules, logEntry.Category, logEntry.LogLevel, logEntry.EventId, out ProbabilisticSamplerFilterRule? rule);
        if (rule is not null)
        {
            probability = rule.Probability;
            return true;
        }

        return false;
    }
}
