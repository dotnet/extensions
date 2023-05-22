// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Microsoft.Extensions.Resilience.Options;
using Polly.Contrib.WaitAndRetry;
using Xunit;

namespace Microsoft.Extensions.Resilience.Internal.Test;

public class ValidationHelperTests
{
    [Fact]
    public void GetExponentialWithJitterDeterministicDelay_ShouldReturnRetryPolicyUpperboundDelaySum()
    {
        var retryPolicyOptions = new RetryPolicyOptions<string>
        {
            RetryCount = 3,
            BaseDelay = TimeSpan.FromSeconds(2),
            BackoffType = BackoffType.ExponentialWithJitter
        };
        var upperbound = ValidationHelper.GetExponentialWithJitterDeterministicDelay(retryPolicyOptions);
        var jitteredDelays = Backoff.DecorrelatedJitterBackoffV2(retryPolicyOptions.BaseDelay, retryPolicyOptions.RetryCount);

        var expected = TimeSpan.FromTicks(114_061_988);
        Assert.True(upperbound >= jitteredDelays.Aggregate((accumulated, current) => accumulated + current));
        Assert.Equal(expected.TotalMilliseconds, upperbound.TotalMilliseconds);
    }

    [Fact]
    public void GetExponentialWithJitterDeterministicDelay_MaxDelayTest()
    {
        var options = new RetryPolicyOptions<string>
        {
            RetryCount = 99,
            BaseDelay = TimeSpan.FromDays(1),
            BackoffType = BackoffType.ExponentialWithJitter
        };

        var upper = ValidationHelper.GetRetryPolicyDelaySum(options);

        Assert.Equal(TimeSpan.MaxValue.Ticks - 1000, upper.Ticks);
    }

    [Fact]
    public void GetRetryPolicyDelaySum_Ok()
    {
        var options = new RetryPolicyOptions<string>
        {
            RetryCount = 2,
            BaseDelay = TimeSpan.FromSeconds(2),
            BackoffType = BackoffType.Linear
        };

        Assert.Equal(TimeSpan.FromSeconds(6), options.GetRetryPolicyDelaySum());
    }
}
