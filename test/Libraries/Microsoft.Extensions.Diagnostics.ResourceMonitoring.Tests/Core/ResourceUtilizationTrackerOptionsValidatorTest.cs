// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public sealed class ResourceUtilizationTrackerOptionsValidatorTest
{
    [Fact]
    public void Validator_GivenValidOptions_Succeeds()
    {
        var options = new ResourceMonitoringOptions
        {
            CollectionWindow = TimeSpan.FromMilliseconds(100),
            SamplingInterval = TimeSpan.FromMilliseconds(10),
            PublishingWindow = TimeSpan.FromSeconds(200)
        };

        var validator = new ResourceMonitoringOptionsValidator();

        var isValid = validator.Validate(nameof(options), options).Succeeded;

        Assert.True(isValid);
    }

    [Fact]
    public void Validator_GivenOptionsWithInvalidSamplingWindow_Fails()
    {
        var options = new ResourceMonitoringOptions
        {
            CollectionWindow = TimeSpan.FromTicks(1),
            SamplingInterval = TimeSpan.FromSeconds(1),
            PublishingWindow = TimeSpan.FromSeconds(200)
        };

        var validator = new ResourceMonitoringOptionsValidator();

        Assert.True(validator.Validate(nameof(options), options).Failed);
    }

    [Fact]
    public void Validator_GivenOptionsWithInvalidSamplingPeriod_Fails()
    {
        var options = new ResourceMonitoringOptions
        {
            CollectionWindow = TimeSpan.FromMilliseconds(100),
            SamplingInterval = TimeSpan.FromMilliseconds(0),
            PublishingWindow = TimeSpan.FromSeconds(200)
        };

        var validator = new ResourceMonitoringOptionsValidator();

        Assert.True(validator.Validate(nameof(options), options).Failed);
    }

    [Fact]
    public void Validator_GivenOptionsWithInvalidMinimalRetentionPeriod_Fails()
    {
        var options = new ResourceMonitoringOptions
        {
            CollectionWindow = TimeSpan.FromMilliseconds(100),
            SamplingInterval = TimeSpan.FromMilliseconds(1),
            PublishingWindow = TimeSpan.FromSeconds(-5)
        };

        var validator = new ResourceMonitoringOptionsValidator();

        Assert.True(validator.Validate(nameof(options), options).Failed);
    }

    [Theory]
    [InlineData(-100, 1, ResourceMonitoringOptions.MinimumSamplingWindow, true)]
    [InlineData(-1, 1, ResourceMonitoringOptions.MinimumSamplingWindow, true)]
    [InlineData(0, -100, ResourceMonitoringOptions.MinimumSamplingWindow, true)]
    [InlineData(0, -1, ResourceMonitoringOptions.MinimumSamplingWindow, true)]
    [InlineData(0, 0, ResourceMonitoringOptions.MinimumSamplingWindow, true)]
    [InlineData(0, ResourceMonitoringOptions.MinimumSamplingPeriod + 1, ResourceMonitoringOptions.MinimumSamplingWindow, true)]
    [InlineData(0, 1, ResourceMonitoringOptions.MinimumSamplingWindow, true)]
    [InlineData(1, 1, ResourceMonitoringOptions.MinimumSamplingWindow, true)]
    [InlineData(ResourceMonitoringOptions.MinimumSamplingWindow, 1, ResourceMonitoringOptions.MinimumSamplingWindow, false)]
    [InlineData(ResourceMonitoringOptions.MinimumSamplingWindow, 100, ResourceMonitoringOptions.MinimumSamplingWindow, false)]
    [InlineData(
        ResourceMonitoringOptions.MaximumSamplingWindow - 1,
        ResourceMonitoringOptions.MaximumSamplingPeriod - 1,
        ResourceMonitoringOptions.MaximumSamplingPeriod - 1,
        false)]
    [InlineData(
        ResourceMonitoringOptions.MaximumSamplingWindow,
        ResourceMonitoringOptions.MaximumSamplingPeriod,
        ResourceMonitoringOptions.MaximumSamplingPeriod,
        false)]
    [InlineData(
        ResourceMonitoringOptions.MinimumSamplingWindow,
        ResourceMonitoringOptions.MinimumSamplingPeriod,
        -1,
        true)]
    [InlineData(
        ResourceMonitoringOptions.MinimumSamplingWindow,
        ResourceMonitoringOptions.MinimumSamplingPeriod,
        ResourceMonitoringOptions.MaximumSamplingWindow + 1,
        true)]
    public void Validator_With_Multiple_Options_Scenarios(int samplingWindow, int samplingPeriod, int calculationPeriod, bool isError)
    {
        var options = new ResourceMonitoringOptions
        {
            CollectionWindow = TimeSpan.FromMilliseconds(samplingWindow),
            SamplingInterval = TimeSpan.FromMilliseconds(samplingPeriod),
            PublishingWindow = TimeSpan.FromMilliseconds(calculationPeriod)
        };

        var validator = new ResourceMonitoringOptionsValidator();

        Assert.Equal(isError, validator.Validate(nameof(options), options).Failed);
    }
}
