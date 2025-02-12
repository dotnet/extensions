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

public class ProbabilisticSamplerTests
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
        ProbabilisticSamplerOptions options = new();
        options.Rules.Add(new ProbabilisticSamplerFilterRule(probability, null, LogLevel.Trace, null));
        var sampler = new ProbabilisticSampler(new StaticOptionsMonitor<ProbabilisticSamplerOptions>(options));

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
        var logRecordParameters = new LogEntry<IReadOnlyList<KeyValuePair<string, object?>>>(
            LogLevel.Warning, nameof(WhenParametersNotMatch_AlwaysSamples), 0, _dummyState, _dummyException, _dummyFormatter);
        ProbabilisticSamplerOptions options = new();
        options.Rules.Add(new ProbabilisticSamplerFilterRule(Probability, null, LogLevel.Information, null));
        var sampler = new ProbabilisticSampler(new StaticOptionsMonitor<ProbabilisticSamplerOptions>(options));

        // Act
        var actualDecision = sampler.ShouldSample(logRecordParameters);

        // Assert
        Assert.True(actualDecision);
    }

    [Fact]
    public void WhenParametersMatch_UsesProvidedProbability()
    {
        // Arrange
        const double Probability = 0.0;
        var logRecordParameters = new LogEntry<IReadOnlyList<KeyValuePair<string, object?>>>(
            LogLevel.Information, nameof(WhenParametersMatch_UsesProvidedProbability), 0, _dummyState, _dummyException, _dummyFormatter);
        ProbabilisticSamplerOptions options = new();
        options.Rules.Add(new ProbabilisticSamplerFilterRule(Probability, null, LogLevel.Information, null));
        var sampler = new ProbabilisticSampler(new StaticOptionsMonitor<ProbabilisticSamplerOptions>(options));

        // Act
        var actualDecision = sampler.ShouldSample(logRecordParameters);

        // Assert
        Assert.False(actualDecision);
    }
}
