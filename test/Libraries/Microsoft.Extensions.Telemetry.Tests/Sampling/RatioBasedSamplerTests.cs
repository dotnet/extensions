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
        var sampler = new RatioBasedSampler(probability, LogLevel.Trace);

        // Act
        var actualDecision = sampler.ShouldSample(new SamplingParameters(LogLevel.Trace, nameof(SamplesAsConfigured), 0));

        // Assert
        Assert.Equal(expectedSamplingDecision, actualDecision);
    }

    [Fact]
    public void WhenParametersNotMatch_AlwaysSamples()
    {
        const double Probability = 0.0;
        var logRecordParameters = new SamplingParameters(LogLevel.Warning, nameof(WhenParametersNotMatch_AlwaysSamples), 0);

        // Arrange
        var sampler = new RatioBasedSampler(Probability, LogLevel.Information);

        // Act
        var actualDecision = sampler.ShouldSample(logRecordParameters);

        // Assert
        Assert.True(actualDecision);
    }

    [Fact]
    public void WhenParametersMatch_UsesProvidedProbability()
    {
        const double Probability = 1.0;
        var logRecordParameters = new SamplingParameters(LogLevel.Information, nameof(WhenParametersMatch_UsesProvidedProbability), 0);

        // Arrange
        var sampler = new RatioBasedSampler(Probability, LogLevel.Information);

        // Act
        var actualDecision = sampler.ShouldSample(logRecordParameters);

        // Assert
        Assert.True(actualDecision);
    }
}
