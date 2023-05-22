// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Polly.Timeout;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class TimeoutPolicyOptionsTests
{
    private readonly TimeoutPolicyOptions _testClass;

    public TimeoutPolicyOptionsTests()
    {
        _testClass = new TimeoutPolicyOptions();
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var instance = new TimeoutPolicyOptions();
        Assert.NotNull(instance);
    }

    [Fact]
    public void TimeoutIntervalProperty_ValidValue_ShouldGetAndSet()
    {
        var testValue = new TimeSpan(234);
        _testClass.TimeoutInterval = testValue;
        Assert.Equal(testValue, _testClass.TimeoutInterval);
        OptionsUtilities.ValidateOptions(_testClass);
    }

    [Fact]
    public void TimeoutStrategyProperty_ValidValue_ShouldGetAndSet()
    {
        _testClass.TimeoutStrategy = TimeoutStrategy.Pessimistic;
        Assert.Equal(TimeoutStrategy.Pessimistic, _testClass.TimeoutStrategy);
        OptionsUtilities.ValidateOptions(_testClass);
    }

    [Fact]
    public void TimeoutIntervalProperty_InvalidValue_ShouldThrow()
    {
        var testValue = new TimeSpan(-2);
        _testClass.TimeoutInterval = testValue;
        Assert.Equal(testValue, _testClass.TimeoutInterval);
        Assert.Throws<ValidationException>(() =>
            OptionsUtilities.ValidateOptions(_testClass));
    }

    [Fact]
    public void DefaultInstance_ShouldInitializeWithDefault()
    {
        var defaultConfiguration = Constants.TimeoutPolicy.DefaultOptions;

        Assert.NotNull(defaultConfiguration);
        Assert.Equal(Constants.TimeoutPolicy.DefaultOptions.TimeoutInterval, defaultConfiguration.TimeoutInterval);
        Assert.NotNull(defaultConfiguration.OnTimedOutAsync);
    }

    [Fact]
    public void OnTimedOutAsyncSet_NullValue_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _testClass.OnTimedOutAsync = null!);
    }

    [Fact]
    public void OnTimedOutAsync_ValidValue_ShouldGetAndSet()
    {
        Func<TimeoutTaskArguments, Task> testValue = _ => Task.CompletedTask;

        _testClass.OnTimedOutAsync = testValue;
        Assert.Equal(testValue, _testClass.OnTimedOutAsync);
    }
}
