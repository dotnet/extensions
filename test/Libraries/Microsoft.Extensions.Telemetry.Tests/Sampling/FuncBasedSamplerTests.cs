// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Sampling;

public class FuncBasedSamplerTests
{
    [Theory]
    [CombinatorialData]
    public void SamplesAsConfigured(bool configuredDecision)
    {
        // Arrange
        var sampler = new FuncBasedSampler((_) => configuredDecision);

        // Act
        var actualDecision = sampler.ShouldSample(new SamplingParameters(null, null, null));

        // Assert
        Assert.Equal(configuredDecision, actualDecision);
    }
}
