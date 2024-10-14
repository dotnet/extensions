// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Samples logs according to the specified probability.
/// </summary>
public class GlobalRatioBasedSampler : LoggerSampler
{
    private readonly int _sampleRate;
    private readonly SamplingParameters _parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalRatioBasedSampler"/> class.
    /// </summary>
    /// <param name="sampleRate">rate.</param>
    /// <param name="category">cat.</param>
    /// <param name="eventId">eventId.</param>
    /// <param name="logLevel">level.</param>
    public GlobalRatioBasedSampler(double sampleRate, string? category, EventId? eventId, LogLevel? logLevel)
    {
        _sampleRate = (int)(sampleRate * 100);
        _parameters = new SamplingParameters(category, eventId, logLevel);
    }

    /// <inheritdoc/>
    public override bool ShouldSample(in SamplingParameters parameters)
    {
        // TODO: compare parameters with _parameters

#if NET6_0_OR_GREATER
        return RandomNumberGenerator.GetInt32(101) < _sampleRate;
#else
        return new Random().Next(100) < _sampleRate;
#endif
    }
}
