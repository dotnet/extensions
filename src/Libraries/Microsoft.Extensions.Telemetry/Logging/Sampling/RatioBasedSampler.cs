// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Samples logs according to the specified probability.
/// </summary>
internal class RatioBasedSampler : LoggerSampler
{
    private const int Hundred = 100;

    private readonly int _sampleRate;
    private readonly SamplingParameters _parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="RatioBasedSampler"/> class.
    /// </summary>
    /// <param name="sampleRate">rate.</param>
    /// <param name="category">cat.</param>
    /// <param name="eventId">eventId.</param>
    /// <param name="logLevel">level.</param>
    public RatioBasedSampler(double sampleRate, LogLevel? logLevel, string? category, EventId? eventId)
    {
        _sampleRate = (int)(sampleRate * Hundred);
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
        return RandomNumberGenerator.GetInt32(Hundred + 1) < _sampleRate;
#else
        return new Random().Next(Hundred) < _sampleRate;
#endif
    }

    private bool IsApplicable(SamplingParameters parameters)
    {
        if (parameters.LogLevel > _parameters.LogLevel)
        {
            return false;
        }

        if (parameters.EventId != _parameters.EventId)
        {
            return false;
        }

        if (parameters.Category != _parameters.Category)
        {
            return false;
        }

        return true;
    }
}
