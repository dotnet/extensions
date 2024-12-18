// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Sampling;

#pragma warning disable CA5394 // Do not use insecure randomness
/// <summary>
/// Samples logs according to the specified probability.
/// </summary>
internal sealed class ProbabilitySampler : LoggerSampler
{
#if !NET6_0_OR_GREATER
    private static readonly System.Threading.ThreadLocal<Random> _randomInstance = new(() => new Random());
#endif

    private readonly IOptionsMonitor<ProbabilitySamplerOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProbabilitySampler"/> class.
    /// </summary>
    public ProbabilitySampler(IOptionsMonitor<ProbabilitySamplerOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public override bool ShouldSample(SamplingParameters parameters)
    {
        if (!TryApply(parameters, out var probability))
        {
            return true;
        }

#if NET6_0_OR_GREATER
        return Random.Shared.Next(int.MaxValue) < int.MaxValue * probability;
#else
        return _randomInstance.Value!.Next(int.MaxValue) < int.MaxValue * probability;
#endif
    }

    private bool TryApply(SamplingParameters parameters, out double probability)
    {
        probability = 0.0;

        // TO DO: check if we can optimize this. It is a hot path and
        // we should be able to minimize number of rule selections on every log record.
        SamplerRuleSelector.Select(_options.CurrentValue.Rules, parameters.Category, parameters.LogLevel, parameters.EventId, out ProbabilitySamplerFilterRule? rule);
        if (rule is not null)
        {
            probability = rule.Probability;
            return true;
        }

        return false;
    }
}
