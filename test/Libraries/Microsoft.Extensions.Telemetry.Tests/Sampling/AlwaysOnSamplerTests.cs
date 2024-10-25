// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Sampling;

public class AlwaysOnSamplerTests
{
    [Fact]
    public void AlwaysSamples()
    {
        // Arrange
        var sampler = new AlwaysOnSampler();

        // Act
        var shouldSample = sampler.ShouldSample(new SamplingParameters(null, null, null));

        // Assert
        Assert.True(shouldSample);
    }
}
