// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Samples logs according to the specified probability.
/// </summary>
public class RatioBasedSampler : LoggerSampler
{
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
        _sampleRate = (int)(sampleRate * 100);
        _parameters = new SamplingParameters(logLevel, category, eventId);
    }

    /// <inheritdoc/>
    public override bool ShouldSample(SamplingParameters parameters)
    {
        // TODO: compare parameters with _parameters

#if NET6_0_OR_GREATER
        return RandomNumberGenerator.GetInt32(101) < _sampleRate;
#else
        return new Random().Next(100) < _sampleRate;
#endif
    }
}
