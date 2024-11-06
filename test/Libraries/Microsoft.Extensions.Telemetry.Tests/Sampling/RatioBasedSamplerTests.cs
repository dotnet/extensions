// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Xunit;
using static Microsoft.Extensions.Logging.Test.ExtendedLoggerTests;

namespace Microsoft.Extensions.Telemetry.Sampling;

public class RatioBasedSamplerTests
{
    [Theory]
    [InlineData(1.0, true)]
    [InlineData(0.0, false)]
    public void SamplesAsConfigured(double probability, bool expectedSamplingDecision)
    {
        // Arrange
        RatioBasedSamplerOptions options = new();
        options.Rules.Add(new RatioBasedSamplerFilterRule(probability, null, LogLevel.Trace, null, null));
        var sampler = new RatioBasedSampler(new StaticOptionsMonitor<RatioBasedSamplerOptions>(options));

        // Act
        var actualDecision = sampler.ShouldSample(new SamplingParameters(LogLevel.Trace, nameof(SamplesAsConfigured), 0));

        // Assert
        Assert.Equal(expectedSamplingDecision, actualDecision);
    }

    [Fact]
    public void WhenParametersNotMatch_AlwaysSamples()
    {
        // Arrange
        const double Probability = 0.0;
        var logRecordParameters = new SamplingParameters(LogLevel.Warning, nameof(WhenParametersNotMatch_AlwaysSamples), 0);
        RatioBasedSamplerOptions options = new();
        options.Rules.Add(new RatioBasedSamplerFilterRule(Probability, null, LogLevel.Information, null, null));
        var sampler = new RatioBasedSampler(new StaticOptionsMonitor<RatioBasedSamplerOptions>(options));

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
        var logRecordParameters = new SamplingParameters(LogLevel.Information, nameof(WhenParametersMatch_UsesProvidedProbability), 0);
        RatioBasedSamplerOptions options = new();
        options.Rules.Add(new RatioBasedSamplerFilterRule(Probability, null, LogLevel.Information, null, null));
        var sampler = new RatioBasedSampler(new StaticOptionsMonitor<RatioBasedSamplerOptions>(options));

        // Act
        var actualDecision = sampler.ShouldSample(logRecordParameters);

        // Assert
        Assert.False(actualDecision);
    }
}
