// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Resilience.Options;
using Polly.Contrib.WaitAndRetry;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test;

public class RetryOptionsExtensionsTests
{
    private const int DefaultRetryCount = 2;
    private static readonly TimeSpan _defaultBackoffDelay = TimeSpan.FromSeconds(1);

    private readonly RetryPolicyOptions<string> _testClass;

    public RetryOptionsExtensionsTests()
    {
        _testClass = new RetryPolicyOptions<string>
        {
            BaseDelay = _defaultBackoffDelay,
            RetryCount = DefaultRetryCount
        };
    }

    [Fact]
    public void GetDelays_ExponentialWithJitter_ShouldReturnJitterBackoff()
    {
        _testClass.BackoffType = BackoffType.ExponentialWithJitter;
        var delays = _testClass.GetDelays().ToArray();

        Assert.NotEmpty(delays);
        Assert.Equal(DefaultRetryCount, delays.Length);
        foreach (var entry in delays)
        {
            Assert.True(entry.TotalSeconds > 0);
        }
    }

    [InlineData(BackoffType.ExponentialWithJitter, false)]
    [InlineData(BackoffType.Constant, true)]
    [InlineData(BackoffType.Linear, true)]
    [Theory]
    public void GetDelays_MultipleEnumeration_EnsureExpectedDelays(BackoffType type, bool shouldBeEqual)
    {
        _testClass.BackoffType = type;
        var source = _testClass.GetDelays();
        var delays1 = source.ToArray();
        var delays2 = source.ToArray();

        Assert.NotEmpty(delays1);
        Assert.Equal(delays1.Length, delays2.Length);
        Assert.Equal(shouldBeEqual, delays1.SequenceEqual(delays2));
        Assert.Equal(shouldBeEqual, source is List<TimeSpan>);
    }

    [Fact]
    public void GetDelays_Linear_ShouldReturnLinearBackoff()
    {
        _testClass.BackoffType = BackoffType.Linear;
        var expectedDelay = Backoff.LinearBackoff(_defaultBackoffDelay, DefaultRetryCount);
        var delay = _testClass.GetDelays();
        Assert.Equal(expectedDelay, delay);
    }

    [Fact]
    public void GetDelays_Constant_ShouldReturnConstantBackoff()
    {
        _testClass.BackoffType = BackoffType.Constant;
        var expectedDelay = Backoff.ConstantBackoff(_defaultBackoffDelay, DefaultRetryCount);
        var delay = _testClass.GetDelays();
        Assert.Equal(expectedDelay, delay);
    }

    [Fact]
    public void GetDelays_Other_ShouldThrow()
    {
        _testClass.BackoffType = (BackoffType)5;
        Assert.Throws<InvalidOperationException>(() => _testClass.GetDelays());
    }

    [Fact]
    public void GetDelays_Single_ShouldReturnSingleDelay()
    {
        _testClass.BackoffType = BackoffType.Constant;
        var newDelay = TimeSpan.FromSeconds(1);
        var expectedDelay = Backoff.ConstantBackoff(newDelay, 1).Single();
        var delay = _testClass.GetDelays().First();
        Assert.Equal(expectedDelay, delay);
    }

    [Fact]
    public void GetDelays_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => RetryPolicyOptionsExtensions.GetDelays(null!));
    }
}
