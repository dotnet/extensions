// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Resilience.Options;
using Xunit;
namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class CircuitBreakerPolicyOptionsTestsNonGeneric
{
    private readonly CircuitBreakerPolicyOptions _testClass;

    public CircuitBreakerPolicyOptionsTestsNonGeneric()
    {
        _testClass = new CircuitBreakerPolicyOptions();
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var instance = new CircuitBreakerPolicyOptions();
        Assert.NotNull(instance);
    }

    [Fact]
    public void FailureThreshold_ValidValue_ShouldGetAndSet()
    {
        const double TestValue = .09;
        _testClass.FailureThreshold = TestValue;

        Assert.Equal(TestValue, _testClass.FailureThreshold);
        OptionsUtilities.ValidateOptions(_testClass);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.1)]
    [InlineData(0.0)]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void FailureThreshold_InvalidValue_ShouldThrow(double testValue)
    {
        _testClass.FailureThreshold = testValue;
        Assert.Throws<ValidationException>(() =>
            OptionsUtilities.ValidateOptions(_testClass));
    }

    [Fact]
    public void MinimumThroughput_ValidValue_ShouldGetAndSet()
    {
        const int TestValue = 931_955_621;
        _testClass.MinimumThroughput = TestValue;

        Assert.Equal(TestValue, _testClass.MinimumThroughput);
        OptionsUtilities.ValidateOptions(_testClass);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void MinimumThroughput_InvalidValue_ShouldThrow(int testValue)
    {
        _testClass.MinimumThroughput = testValue;
        Assert.Throws<ValidationException>(() =>
            OptionsUtilities.ValidateOptions(_testClass));
    }

    [Fact]

    public void BreakDuration_ValidValue_ShouldGetAndSet()
    {
        var testValue = TimeSpan.FromMilliseconds(567);
        _testClass.BreakDuration = testValue;
        Assert.Equal(testValue, _testClass.BreakDuration);
        OptionsUtilities.ValidateOptions(_testClass);
    }

    [Fact]
    public void BreakDuration_SetInvalidValue_ShouldThrow()
    {
        var testValue = TimeSpan.FromDays(-1);
        _testClass.BreakDuration = testValue;
        Assert.Throws<ValidationException>(() =>
            OptionsUtilities.ValidateOptions(_testClass));
    }

    [Fact]
    public void SamplingDuration_ValidValueShouldGetAndSet()
    {
        var testValue = TimeSpan.FromMilliseconds(567);
        _testClass.SamplingDuration = testValue;

        Assert.Equal(testValue, _testClass.SamplingDuration);
        OptionsUtilities.ValidateOptions(_testClass);
    }

    [Fact]
    public void SamplingDuration_InvalidValue_ShouldThrow()
    {
        var testValue = TimeSpan.Zero;
        _testClass.SamplingDuration = testValue;
        Assert.Throws<ValidationException>(() =>
            OptionsUtilities.ValidateOptions(_testClass));
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
    public void OnCircuitBreak_ValidValue_ShouldGetAndSet()
    {
        Action<BreakActionArguments> testValue = _ => { };

        _testClass.OnCircuitBreak = testValue;
        Assert.Equal(testValue, _testClass.OnCircuitBreak);
    }

    [Fact]
    public void OnCircuitBreakSet_NullValue_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _testClass.OnCircuitBreak = null!);
    }

    [Fact]
    public void OnCircuitReset_ValidValue_ShouldGetAndSet()
    {
        Action<ResetActionArguments> testValue = _ => { };
        _testClass.OnCircuitReset = testValue;

        Assert.Equal(testValue, _testClass.OnCircuitReset);
    }
}
