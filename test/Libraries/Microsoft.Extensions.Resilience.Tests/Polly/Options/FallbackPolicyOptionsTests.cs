// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Xunit;
namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class FallbackPolicyOptionsTests
{
    private readonly FallbackPolicyOptions<string> _testClass;

    public FallbackPolicyOptionsTests()
    {
        _testClass = new FallbackPolicyOptions<string>();
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        Assert.NotNull(_testClass);
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
    public void OnFallback_ValidValue_ShouldGetAndSet()
    {
        Func<FallbackTaskArguments<string>, Task> testValue = _ => Task.CompletedTask;

        _testClass.OnFallbackAsync = testValue;
        Assert.Equal(testValue, _testClass.OnFallbackAsync);
    }

    [Fact]
    public void OnFallbackSet_NullValue_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _testClass.OnFallbackAsync = null!);
    }
}
