// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class FallbackPolicyOptionsTestsNonGeneric
{
    private readonly FallbackPolicyOptions _testClass;

    public FallbackPolicyOptionsTestsNonGeneric()
    {
        _testClass = new FallbackPolicyOptions();
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        Assert.NotNull(_testClass);
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
        Func<FallbackTaskArguments, Task> testValue = _ => Task.CompletedTask;

        _testClass.OnFallbackAsync = testValue;
        Assert.Equal(testValue, _testClass.OnFallbackAsync);
    }

    [Fact]
    public void OnFallbackSet_NullValue_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _testClass.OnFallbackAsync = null!);
    }
}
