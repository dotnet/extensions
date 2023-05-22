// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class RetryPolicyOptionsTests
{
    private readonly RetryPolicyOptions<string> _testClass;

    public RetryPolicyOptionsTests()
    {
        _testClass = new RetryPolicyOptions<string>();
    }

    [Fact]
    public void Default_RetryPolicyOptions_Returns_Completed_Task()
    {
        var result = RetryPolicyOptions<string>.DefaultOnRetryAsync(default);

        Assert.Equal(Task.CompletedTask, result);
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var instance = new RetryPolicyOptions<string>();
        Assert.NotNull(instance);
    }

    [Fact]
    public void RetryCount_ShouldGetAndSet()
    {
        const int TestValue = 100;
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
    public void DelayGenerators_ShouldGetNullDefault()
    {
        Assert.Null(_testClass.RetryDelayGenerator);
    }

    [Fact]
    public void ShouldHandleResultAsError_ValidValue_ShouldGetAndSet()
    {
        Predicate<string> testValue = _ => true;
        _testClass.ShouldHandleResultAsError = testValue;
        Assert.Equal(testValue, _testClass.ShouldHandleResultAsError);
    }

    [Fact]
    public void ShouldHandleResultAsError_DefaultValue_ShouldReturnFalse()
    {
        var shouldHandle = _testClass.ShouldHandleResultAsError(string.Empty);
        Assert.False(shouldHandle);
    }

    [Fact]
    public void ShouldHandleResultAsError_NullValue_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _testClass.ShouldHandleResultAsError = null!);
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
    public void OnRetryAsync_ValidValue_ShouldGetAndSet()
    {
        Func<RetryActionArguments<string>, Task> testValue = _ => Task.CompletedTask;

        _testClass.OnRetryAsync = testValue;
        Assert.Equal(testValue, _testClass.OnRetryAsync);
    }

    [Fact]
    public void OnRetryAsyncSet_NullValue_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _testClass.OnRetryAsync = null!);
    }
}
