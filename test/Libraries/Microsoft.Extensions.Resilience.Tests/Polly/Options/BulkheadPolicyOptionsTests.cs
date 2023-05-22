// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Xunit;
namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public class BulkheadPolicyOptionsTests
{
    private readonly BulkheadPolicyOptions _testClass;

    public BulkheadPolicyOptionsTests()
    {
        _testClass = new BulkheadPolicyOptions();
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var instance = new BulkheadPolicyOptions();
        Assert.NotNull(instance);
    }

    [Fact]
    public void MaxConcurrencyProperty_ShouldGetAndSet()
    {
        var testValue = 100;
        _testClass.MaxConcurrency = testValue;
        Assert.Equal(testValue, _testClass.MaxConcurrency);
    }

    [Fact]
    public void MaxQueuedActionsProperty_ShouldGetAndSet()
    {
        var testValue = 1;
        _testClass.MaxQueuedActions = testValue;
        Assert.Equal(testValue, _testClass.MaxQueuedActions);
    }

    [Fact]
    public void DefaultInstance_ShouldInitializeWithDefault()
    {
        var defaultConfiguration = Constants.BulkheadPolicy.DefaultOptions;
        Assert.NotNull(defaultConfiguration);
        Assert.Equal(1000, defaultConfiguration.MaxConcurrency);
        Assert.Equal(Constants.BulkheadPolicy.DefaultOptions.MaxQueuedActions, defaultConfiguration.MaxQueuedActions);
    }

    [Fact]
    public void OnRejected_ValidValue_ShouldGetAndSet()
    {
        Func<BulkheadTaskArguments, Task> testValue = _ => Task.CompletedTask;

        _testClass.OnBulkheadRejectedAsync = testValue;
        Assert.Equal(testValue, _testClass.OnBulkheadRejectedAsync);
    }

    [Fact]
    public void OnRejectedSet_NullValue_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => _testClass.OnBulkheadRejectedAsync = null!);
    }

    [Fact]
    public async Task OnRejected_DefaultValue_ShouldBeInitialized()
    {
        var instance = new BulkheadPolicyOptions();
        Assert.NotNull(instance);

        var function = instance.OnBulkheadRejectedAsync;
        Assert.NotNull(function);

        var args = default(BulkheadTaskArguments);
        await function(args);
    }
}
