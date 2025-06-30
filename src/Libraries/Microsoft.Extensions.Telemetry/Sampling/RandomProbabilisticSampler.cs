// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
#if !NETFRAMEWORK
using System.Security.Cryptography;
#endif
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Sampling;

#pragma warning disable CA5394 // Do not use insecure randomness - acceptable for the purposes of sampling

/// <summary>
/// Randomly samples logs according to the specified probability.
/// </summary>
internal sealed class RandomProbabilisticSampler : LoggingSampler, IDisposable
{
    internal RandomProbabilisticSamplerFilterRule[] LastKnownGoodSamplerRules;

#if NETFRAMEWORK
    private static readonly System.Threading.ThreadLocal<Random> _randomInstance = new(() => new Random());
#endif

    private readonly IDisposable? _samplerOptionsChangeTokenRegistration;
    private readonly LogSamplingRuleSelector<RandomProbabilisticSamplerFilterRule> _ruleSelector;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomProbabilisticSampler"/> class.
    /// </summary>
    public RandomProbabilisticSampler(
        LogSamplingRuleSelector<RandomProbabilisticSamplerFilterRule> ruleSelector,
        IOptionsMonitor<RandomProbabilisticSamplerOptions> options)
    {
        _ruleSelector = Throw.IfNull(ruleSelector);
        LastKnownGoodSamplerRules = Throw.IfNullOrMemberNull(options, options.CurrentValue).Rules.ToArray();
        _samplerOptionsChangeTokenRegistration = options.OnChange(OnSamplerOptionsChanged);
    }

    /// <inheritdoc/>
    public override bool ShouldSample<TState>(in LogEntry<TState> logEntry)
    {
        if (!TryApply(logEntry, out double probability))
        {
            return true;
        }

#if NETFRAMEWORK
        return _randomInstance.Value!.Next(int.MaxValue) < int.MaxValue * probability;
#else
        return RandomNumberGenerator.GetInt32(int.MaxValue) < int.MaxValue * probability;
#endif
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            _samplerOptionsChangeTokenRegistration?.Dispose();
        }
    }

    private void OnSamplerOptionsChanged(RandomProbabilisticSamplerOptions? updatedOptions)
    {
        if (updatedOptions is null)
        {
            LastKnownGoodSamplerRules = Array.Empty<RandomProbabilisticSamplerFilterRule>();
        }
        else
        {
            LastKnownGoodSamplerRules = updatedOptions.Rules.ToArray();
        }

        _ruleSelector.InvalidateCache();
    }

    private bool TryApply<TState>(in LogEntry<TState> logEntry, out double probability)
    {
        probability = 0.0;

        RandomProbabilisticSamplerFilterRule? rule = _ruleSelector.Select(
            LastKnownGoodSamplerRules,
            logEntry.Category,
            logEntry.LogLevel,
            logEntry.EventId);

        if (rule is null)
        {
            return false;
        }

        probability = rule.Probability;
        return true;
    }
}
