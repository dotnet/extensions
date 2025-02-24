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

#pragma warning disable CA5394 // Do not use insecure randomness - not needed for sampling

/// <summary>
/// Randomly samples logs according to the specified probability.
/// </summary>
internal sealed class RandomProbabilisticSampler : LoggingSampler, IDisposable
{
#if NETFRAMEWORK
    private static readonly System.Threading.ThreadLocal<Random> _randomInstance = new(() => new Random());
#endif

    private readonly IDisposable? _samplerOptionsChangeTokenRegistration;
    private RandomProbabilisticSamplerFilterRule[] _lastKnownGoodSamplerRules;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomProbabilisticSampler"/> class.
    /// </summary>
    public RandomProbabilisticSampler(IOptionsMonitor<RandomProbabilisticSamplerOptions> options)
    {
        _lastKnownGoodSamplerRules = Throw.IfNullOrMemberNull(options, options!.CurrentValue).Rules.ToArray();
        _samplerOptionsChangeTokenRegistration = options.OnChange(OnSamplerOptionsChanged);
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
            _lastKnownGoodSamplerRules = Array.Empty<RandomProbabilisticSamplerFilterRule>();
        }
        else
        {
            _lastKnownGoodSamplerRules = updatedOptions.Rules.ToArray();
        }
    }

    private bool TryApply<TState>(in LogEntry<TState> logEntry, out double probability)
    {
        probability = 0.0;

        LogSamplingRuleSelector.Select(_lastKnownGoodSamplerRules, logEntry.Category, logEntry.LogLevel, logEntry.EventId, out RandomProbabilisticSamplerFilterRule? rule);
        if (rule is not null)
        {
            probability = rule.Probability;
            return true;
        }

        return false;
    }
}
