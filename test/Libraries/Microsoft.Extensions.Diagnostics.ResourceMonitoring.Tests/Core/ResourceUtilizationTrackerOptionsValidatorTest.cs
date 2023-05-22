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
        var options = new ResourceUtilizationTrackerOptions
        {
            CollectionWindow = TimeSpan.FromMilliseconds(100),
            SamplingInterval = TimeSpan.FromMilliseconds(10),
            CalculationPeriod = TimeSpan.FromSeconds(200)
        };

        var validator = new ResourceUtilizationTrackerOptionsValidator();

        var isValid = validator.Validate(nameof(options), options).Succeeded;

        Assert.True(isValid);
    }

    [Fact]
    public void Validator_GivenOptionsWithInvalidSamplingWindow_Fails()
    {
        var options = new ResourceUtilizationTrackerOptions
        {
            CollectionWindow = TimeSpan.FromTicks(1),
            SamplingInterval = TimeSpan.FromSeconds(1),
            CalculationPeriod = TimeSpan.FromSeconds(200)
        };

        var validator = new ResourceUtilizationTrackerOptionsValidator();

        Assert.True(validator.Validate(nameof(options), options).Failed);
    }

    [Fact]
    public void Validator_GivenOptionsWithInvalidSamplingPeriod_Fails()
    {
        var options = new ResourceUtilizationTrackerOptions
        {
            CollectionWindow = TimeSpan.FromMilliseconds(100),
            SamplingInterval = TimeSpan.FromMilliseconds(0),
            CalculationPeriod = TimeSpan.FromSeconds(200)
        };

        var validator = new ResourceUtilizationTrackerOptionsValidator();

        Assert.True(validator.Validate(nameof(options), options).Failed);
    }

    [Fact]
    public void Validator_GivenOptionsWithInvalidMinimalRetentionPeriod_Fails()
    {
        var options = new ResourceUtilizationTrackerOptions
        {
            CollectionWindow = TimeSpan.FromMilliseconds(100),
            SamplingInterval = TimeSpan.FromMilliseconds(1),
            CalculationPeriod = TimeSpan.FromSeconds(-5)
        };

        var validator = new ResourceUtilizationTrackerOptionsValidator();

        Assert.True(validator.Validate(nameof(options), options).Failed);
    }

    [Theory]
    [InlineData(-100, 1, ResourceUtilizationTrackerOptions.MinimumSamplingWindow, true)]
    [InlineData(-1, 1, ResourceUtilizationTrackerOptions.MinimumSamplingWindow, true)]
    [InlineData(0, -100, ResourceUtilizationTrackerOptions.MinimumSamplingWindow, true)]
    [InlineData(0, -1, ResourceUtilizationTrackerOptions.MinimumSamplingWindow, true)]
    [InlineData(0, 0, ResourceUtilizationTrackerOptions.MinimumSamplingWindow, true)]
    [InlineData(0, ResourceUtilizationTrackerOptions.MinimumSamplingPeriod + 1, ResourceUtilizationTrackerOptions.MinimumSamplingWindow, true)]
    [InlineData(0, 1, ResourceUtilizationTrackerOptions.MinimumSamplingWindow, true)]
    [InlineData(1, 1, ResourceUtilizationTrackerOptions.MinimumSamplingWindow, true)]
    [InlineData(ResourceUtilizationTrackerOptions.MinimumSamplingWindow, 1, ResourceUtilizationTrackerOptions.MinimumSamplingWindow, false)]
    [InlineData(ResourceUtilizationTrackerOptions.MinimumSamplingWindow, 100, ResourceUtilizationTrackerOptions.MinimumSamplingWindow, false)]
    [InlineData(
        ResourceUtilizationTrackerOptions.MaximumSamplingWindow - 1,
        ResourceUtilizationTrackerOptions.MaximumSamplingPeriod - 1,
        ResourceUtilizationTrackerOptions.MaximumSamplingPeriod - 1,
        false)]
    [InlineData(
        ResourceUtilizationTrackerOptions.MaximumSamplingWindow,
        ResourceUtilizationTrackerOptions.MaximumSamplingPeriod,
        ResourceUtilizationTrackerOptions.MaximumSamplingPeriod,
        false)]
    [InlineData(
        ResourceUtilizationTrackerOptions.MinimumSamplingWindow,
        ResourceUtilizationTrackerOptions.MinimumSamplingPeriod,
        -1,
        true)]
    [InlineData(
        ResourceUtilizationTrackerOptions.MinimumSamplingWindow,
        ResourceUtilizationTrackerOptions.MinimumSamplingPeriod,
        ResourceUtilizationTrackerOptions.MaximumSamplingWindow + 1,
        true)]
    public void Validator_With_Multiple_Options_Scenarios(int samplingWindow, int samplingPeriod, int calculationPeriod, bool isError)
    {
        var options = new ResourceUtilizationTrackerOptions
        {
            CollectionWindow = TimeSpan.FromMilliseconds(samplingWindow),
            SamplingInterval = TimeSpan.FromMilliseconds(samplingPeriod),
            CalculationPeriod = TimeSpan.FromMilliseconds(calculationPeriod)
        };

        var validator = new ResourceUtilizationTrackerOptionsValidator();

        Assert.Equal(isError, validator.Validate(nameof(options), options).Failed);
    }
}
