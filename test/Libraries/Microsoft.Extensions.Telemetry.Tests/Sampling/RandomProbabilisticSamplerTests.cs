// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Microsoft.Extensions.Logging.Test.ExtendedLoggerTests;

namespace Microsoft.Extensions.Telemetry.Sampling;

public class RandomProbabilisticSamplerTests
{
    private readonly InvalidOperationException _dummyException = new("test.");
    private readonly IReadOnlyList<KeyValuePair<string, object?>> _dummyState = [];
    private readonly Func<IReadOnlyList<KeyValuePair<string, object?>>, Exception?, string> _dummyFormatter = (_, _) => string.Empty;

    [Theory]
    [InlineData(1.0, true)]
    [InlineData(0.0, false)]
    public void SamplesAsConfigured(double probability, bool expectedSamplingDecision)
    {
        // Arrange
        RandomProbabilisticSamplerOptions options = new();
        options.Rules.Add(new RandomProbabilisticSamplerFilterRule(probability: probability, logLevel: LogLevel.Trace));
        using var sampler = new RandomProbabilisticSampler(new StaticOptionsMonitor<RandomProbabilisticSamplerOptions>(options));

        // Act
        var actualDecision = sampler.ShouldSample(
            new LogEntry<IReadOnlyList<KeyValuePair<string, object?>>>(
                LogLevel.Trace, nameof(SamplesAsConfigured), 0, _dummyState, _dummyException, _dummyFormatter));

        // Assert
        Assert.Equal(expectedSamplingDecision, actualDecision);
    }

    [Fact]
    public void WhenParametersNotMatch_AlwaysSamples()
    {
        // Arrange
        const double Probability = 0.0;
        var logEntry = new LogEntry<IReadOnlyList<KeyValuePair<string, object?>>>(
            LogLevel.Warning, nameof(WhenParametersNotMatch_AlwaysSamples), 0, _dummyState, _dummyException, _dummyFormatter);
        RandomProbabilisticSamplerOptions options = new();
        options.Rules.Add(new RandomProbabilisticSamplerFilterRule(probability: Probability, logLevel: LogLevel.Information));
        using var sampler = new RandomProbabilisticSampler(new StaticOptionsMonitor<RandomProbabilisticSamplerOptions>(options));

        // Act
        var actualDecision = sampler.ShouldSample(logEntry);

        // Assert
        Assert.True(actualDecision);
    }

    [Fact]
    public void WhenParametersMatch_UsesProvidedProbability()
    {
        // Arrange
        const double Probability = 0.0;
        var logEntry = new LogEntry<IReadOnlyList<KeyValuePair<string, object?>>>(
            LogLevel.Information, nameof(WhenParametersMatch_UsesProvidedProbability), 0, _dummyState, _dummyException, _dummyFormatter);
        RandomProbabilisticSamplerOptions options = new();
        options.Rules.Add(new RandomProbabilisticSamplerFilterRule(probability: Probability, logLevel: LogLevel.Information));
        using var sampler = new RandomProbabilisticSampler(new StaticOptionsMonitor<RandomProbabilisticSamplerOptions>(options));

        // Act
        var actualDecision = sampler.ShouldSample(logEntry);

        // Assert
        Assert.False(actualDecision);
    }
}
