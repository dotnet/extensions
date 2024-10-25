// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Sampling;

#pragma warning disable CA5394 // Do not use insecure randomness
/// <summary>
/// Samples logs according to the specified probability.
/// </summary>
internal sealed class RatioBasedSampler : LoggerSampler
{
    private const int Hundred = 100;

#if !NET6_0_OR_GREATER
    private static readonly System.Threading.ThreadLocal<Random> _randomInstance = new(() => new Random());
#endif

    private readonly int _sampleRate;
    private readonly SamplingParameters _parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="RatioBasedSampler"/> class.
    /// </summary>
    /// <param name="probability">The desired probability of sampling. This must be between 0.0 and 1.0.
    /// Higher the value, higher is the probability of a given log record to be sampled in.
    /// </param>
    /// <param name="logLevel">Apply sampling to the provided log level or below.</param>
    /// <param name="category">Log category to apply sampling to.</param>
    /// <param name="eventId">Log event ID to apply sampling to.</param>
    public RatioBasedSampler(double probability, LogLevel? logLevel, string? category, EventId? eventId)
    {
        _sampleRate = (int)(probability * Hundred);
        _parameters = new SamplingParameters(logLevel, category, eventId);
    }

    /// <inheritdoc/>
    public override bool ShouldSample(SamplingParameters parameters)
    {
        if (!IsApplicable(parameters))
        {
            return true;
        }

#if NET6_0_OR_GREATER
        return Random.Shared.Next(Hundred) < _sampleRate;
#else
        return _randomInstance.Value!.Next(Hundred) < _sampleRate;
#endif
    }

    private bool IsApplicable(SamplingParameters parameters)
    {
        if (_parameters.LogLevel is not null && parameters.LogLevel > _parameters.LogLevel)
        {
            return false;
        }

        if (_parameters.Category is not null && parameters.Category != _parameters.Category)
        {
            return false;
        }

        if (_parameters.EventId is not null && parameters.EventId != _parameters.EventId)
        {
            return false;
        }

        return true;
    }
}
