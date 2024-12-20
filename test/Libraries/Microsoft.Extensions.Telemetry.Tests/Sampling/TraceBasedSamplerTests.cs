// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Sampling;
public class TraceBasedSamplerTests
{
    [Fact]
    public void WhenNoActivity_SamplesIn()
    {
        // Arrange
        var sampler = new TraceBasedSampler();

        // Act
        var shouldSample = sampler.ShouldSample(new SamplingParameters(LogLevel.Trace, nameof(WhenNoActivity_SamplesIn), 0));

        // Assert
        Assert.True(shouldSample);
    }

    [Fact]
    public void WhenActivityIsRecorded_SamplesIn()
    {
        // Arrange
        var sampler = new TraceBasedSampler();

        // Act
        using var activity = new Activity("my activity");
        activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        activity.Start();

        var shouldSample = sampler.ShouldSample(new SamplingParameters(LogLevel.Trace, nameof(WhenActivityIsRecorded_SamplesIn), 0));

        // Assert
        Assert.True(shouldSample);

        activity.Stop();
    }

    [Fact]
    public void WhenActivityIsNotRecorded_SamplesOut()
    {
        // Arrange
        var sampler = new TraceBasedSampler();

        // Act
        using var activity = new Activity("my activity");
        activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        activity.Start();

        var shouldSample = sampler.ShouldSample(new SamplingParameters(LogLevel.Trace, nameof(WhenActivityIsNotRecorded_SamplesOut), 0));

        // Assert
        Assert.False(shouldSample);

        activity.Stop();
    }
}
