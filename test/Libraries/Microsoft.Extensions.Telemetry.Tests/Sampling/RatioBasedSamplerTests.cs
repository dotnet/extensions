// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Sampling;

public class RatioBasedSamplerTests
{
    public static IEnumerable<object?[]> MatchedParams()
    {
        yield return new object?[] { LogLevel.Information, null, 0, LogLevel.Information, null, 1 };
        yield return new object?[] { null, "my category", 0, null, "my category", 1 };
        yield return new object?[] { null, null, 1, null, null, 1 };
    }

    public static IEnumerable<object?[]> NotMatchedParams()
    {
        yield return new object?[] { LogLevel.Warning, null, 0, LogLevel.Information, null, 0 };
        yield return new object?[] { null, "my category", 0, null, "another category", 0 };
        yield return new object?[] { null, null, 0, null, null, 1 };
    }

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
    [MemberData(nameof(NotMatchedParams))]
    public void WhenParametersNotMatch_AlwaysSamples(
        LogLevel? logRecordLogLevel, string? logRecordCategory, int? logRecordEventId,
        LogLevel? samplerLogLevel, string? samplerCategory, int? samplerEventId)
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
    [MemberData(nameof(MatchedParams))]
    public void WhenParametersMatch_UsesProvidedProbability(
        LogLevel? logRecordLogLevel, string? logRecordCategory, int? logRecordEventId,
        LogLevel? samplerLogLevel, string? samplerCategory, int? samplerEventId)
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
