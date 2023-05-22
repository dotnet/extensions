// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class RetryPolicyOptionsTestsNonGeneric
{
    private readonly RetryPolicyOptions _testClass;

    public RetryPolicyOptionsTestsNonGeneric()
    {
        _testClass = new RetryPolicyOptions();
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var instance = new RetryPolicyOptions();
        Assert.NotNull(instance);
    }

    [Fact]
    public void RetryCount_ShouldGetAndSet()
    {
        const int TestValue = 1_052_822_497;
        _testClass.RetryCount = TestValue;
        Assert.Equal(TestValue, _testClass.RetryCount);
    }

    [Fact]
    public void BackoffType_ShouldGetAndSet()
    {
        const BackoffType TestValue = BackoffType.ExponentialWithJitter;
        _testClass.BackoffType = TestValue;
        Assert.Equal(TestValue, _testClass.BackoffType);
    }

    [Fact]
    public void BackoffBasedDelay_ShouldGetAndSet()
    {
        var testValue = new TimeSpan(1234);
        _testClass.BaseDelay = testValue;
        Assert.Equal(testValue, _testClass.BaseDelay);
    }

    [Fact]
    public void ShouldHandleException_ValidValue_ShouldGetAndSet()
    {
        Predicate<Exception> testValue = _ => true;
        _testClass.ShouldHandleException = testValue;
        Assert.Equal(testValue, _testClass.ShouldHandleException);
    }

    [Fact]
    public void ShouldHandleException_DefaultValue_ShouldReturnTrue()
    {
        var shouldHandle = _testClass.ShouldHandleException(new AggregateException());
        Assert.True(shouldHandle);
    }

    [Fact]
    public void ShouldHandleException_NullValue_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _testClass.ShouldHandleException = null!);
    }

    [Fact]
    public void OnRetry_ValidValue_ShouldGetAndSet()
    {
        Func<RetryActionArguments, Task> testValue = _ => Task.CompletedTask;

        _testClass.OnRetryAsync = testValue;
        Assert.Equal(testValue, _testClass.OnRetryAsync);
    }

    [Fact]
    public void OnRetrySet_NullValue_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _testClass.OnRetryAsync = null!);
    }
}
