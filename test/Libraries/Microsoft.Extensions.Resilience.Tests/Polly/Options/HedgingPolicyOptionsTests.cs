// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class HedgingPolicyOptionsTests
{
    private readonly HedgingPolicyOptions<string> _testClass;

    public HedgingPolicyOptionsTests()
    {
        _testClass = new HedgingPolicyOptions<string>();
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var instance = new HedgingPolicyOptions<string>();
        Assert.NotNull(instance);
        Assert.NotNull(_testClass.OnHedgingAsync);
        Assert.NotNull(_testClass.ShouldHandleResultAsError);
        Assert.NotNull(_testClass.ShouldHandleException);
        Assert.Null(_testClass.HedgingDelayGenerator);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    public void HedgingDelayGenerator_Can_Set(int seconds)
    {
        var o = new HedgingPolicyOptions<string>();

        var e = Record.Exception(() => o.HedgingDelayGenerator = (_) => TimeSpan.FromSeconds(seconds));

        Assert.Null(e);
        Assert.NotNull(o.HedgingDelayGenerator);
        Assert.Equal(o.HedgingDelayGenerator!(default), TimeSpan.FromSeconds(seconds));
    }

    [Fact]
    public void InfiniteHedgingDelay_CorrectValue()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(-1), HedgingPolicyOptions<string>.InfiniteHedgingDelay);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void HedgingDelayProperty_ValidValue_ShouldGetAndSet(int value)
    {
        var testValue = TimeSpan.FromMilliseconds(value);
        _testClass.HedgingDelay = testValue;

        Assert.Equal(testValue, _testClass.HedgingDelay);
        OptionsUtilities.ValidateOptions(_testClass);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(-3)]
    public void HedgingDelayProperty_InvalidValue_ShouldThrow(int testValue)
    {
        _testClass.HedgingDelay = TimeSpan.FromSeconds(testValue);
        Assert.Throws<ValidationException>(() =>
            OptionsUtilities.ValidateOptions(_testClass));
    }

    [Fact]
    public void MaxHedgedAttemptsProperty_ValidValue_ShouldGetAndSet()
    {
        var testValue = 2;
        _testClass.MaxHedgedAttempts = testValue;

        Assert.Equal(testValue, _testClass.MaxHedgedAttempts);
        OptionsUtilities.ValidateOptions(_testClass);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void MaxHedgedAttemptsProperty_InvalidValue_ShouldThrow(int testValue)
    {
        _testClass.MaxHedgedAttempts = testValue;
        Assert.Throws<ValidationException>(() => OptionsUtilities.ValidateOptions(_testClass));
    }

    [Fact]
    public void DefaultInstance_ShouldInitializeWithDefault()
    {
        var defaultOptions = Constants.HedgingPolicy.DefaultOptions<string>();
        Assert.NotNull(defaultOptions);
        Assert.Equal(TimeSpan.FromSeconds(2), defaultOptions.HedgingDelay);
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
    public void OnHedging_ValidValue_ShouldGetAndSet()
    {
        Func<HedgingTaskArguments<string>, Task> testValue = _ => Task.CompletedTask;

        _testClass.OnHedgingAsync = testValue;
        Assert.Equal(testValue, _testClass.OnHedgingAsync);
    }

    [Fact]
    public void OnHedgingSet_NullValue_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _testClass.OnHedgingAsync = null!);
    }
}
