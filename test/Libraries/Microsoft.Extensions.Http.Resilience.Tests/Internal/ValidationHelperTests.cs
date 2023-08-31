// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Polly;
using Polly.Retry;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Internal;

public class ValidationHelperTests
{
    [Fact]
    public void GetAggregatedDelay_Constant_Ok()
    {
        ValidationHelper.GetAggregatedDelay(
            new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 10,
                BackoffType = DelayBackoffType.Constant,
                Delay = TimeSpan.FromSeconds(1),
            })
            .Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void GetAggregatedDelay_ExponentialWithJitter_ShouldNotBeRandomized()
    {
        var options = new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 10,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromSeconds(1),
        };

        ValidationHelper
            .GetAggregatedDelay(options)
            .Should()
            .Be(ValidationHelper.GetAggregatedDelay(options));
    }

    [Fact]
    public void GetAggregatedDelay_Overflow_Handled()
    {
        var options = new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 99,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromSeconds(1000),
        };

        ValidationHelper
            .GetAggregatedDelay(options)
            .Should()
            .Be(TimeSpan.MaxValue);
    }
}
