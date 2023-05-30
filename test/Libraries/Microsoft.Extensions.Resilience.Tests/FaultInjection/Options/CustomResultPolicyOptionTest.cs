// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test.Options;

public class CustomResultPolicyOptionTest
{
    [Fact]
    public void CanConstruct()
    {
        var instance = new CustomResultPolicyOptions();
        Assert.NotNull(instance);
    }

    [Fact]
    public void CanSetAndGetEnabled()
    {
        var testValue = true;
        var instance = new CustomResultPolicyOptions
        {
            Enabled = testValue
        };
        Assert.Equal(testValue, instance.Enabled);
    }

    [Fact]
    public void CanSetAndGetInjectionRate()
    {
        var testValue = 0.7;
        var instance = new CustomResultPolicyOptions
        {
            FaultInjectionRate = testValue
        };
        Assert.Equal(testValue, instance.FaultInjectionRate);
    }

    [Fact]
    public void CanSetAndGetCustomResultToInject()
    {
        var testValue = "TestCustomResult";
        var instance = new CustomResultPolicyOptions
        {
            CustomResultKey = testValue
        };
        Assert.Equal(testValue, instance.CustomResultKey);
    }

    [Fact]
    public void InstanceHasDefaultValues()
    {
        var instance = new CustomResultPolicyOptions();
        Assert.Equal(ChaosPolicyOptionsBase.DefaultEnabled, instance.Enabled);
        Assert.Equal(ChaosPolicyOptionsBase.DefaultInjectionRate, instance.FaultInjectionRate);
        Assert.Equal(string.Empty, instance.CustomResultKey);
    }
}
