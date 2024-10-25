// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Sampling;

public class RatioBasedSamplerTests
{
    [Theory]
    [InlineData(1.0, true)]
    [InlineData(0.0, false)]
    public void SamplesAsConfigured(double probability, bool expectedSamplingDecision)
    {
        // Arrange
        var sampler = new RatioBasedSampler(probability, null, null, null);

        // Act
        var actualDecision = sampler.ShouldSample(new SamplingParameters(null, null, null));

        // Assert
        Assert.Equal(expectedSamplingDecision, actualDecision);
    }

    [Theory]
    [InlineData(LogLevel.Warning, null, 0, LogLevel.Information, null, 0)]
    [InlineData(null, "my category", 0, null, "another category", 0)]
    [InlineData(null, null, 0, null, null, 1)]
    public void WhenParametersNotMatch_AlwaysSamples(
        LogLevel? logRecordLogLevel, string? logRecordCategory, EventId logRecordEventId,
        LogLevel? samplerLogLevel, string? samplerCategory, EventId samplerEventId)
    {
        const double Probability = 0.0;
        var logRecordParameters = new SamplingParameters(logRecordLogLevel, logRecordCategory, logRecordEventId);

        // Arrange
        var sampler = new RatioBasedSampler(Probability, samplerLogLevel, samplerCategory, samplerEventId);

        // Act
        var actualDecision = sampler.ShouldSample(logRecordParameters);

        // Assert
        Assert.True(actualDecision);
    }

    [Theory]
    [InlineData(LogLevel.Information, null, 0, LogLevel.Information, null, 1)]
    [InlineData(null, "my category", 0, null, "my category", 1)]
    [InlineData(null, null, 1, null, null, 1)]
    public void WhenParametersMatch_UsesProvidedProbability(
        LogLevel? logRecordLogLevel, string? logRecordCategory, EventId logRecordEventId,
        LogLevel? samplerLogLevel, string? samplerCategory, EventId samplerEventId)
    {
        const double Probability = 1.0;
        var logRecordParameters = new SamplingParameters(logRecordLogLevel, logRecordCategory, logRecordEventId);

        // Arrange
        var sampler = new RatioBasedSampler(Probability, samplerLogLevel, samplerCategory, samplerEventId);

        // Act
        var actualDecision = sampler.ShouldSample(logRecordParameters);

        // Assert
        Assert.True(actualDecision);
    }
}
