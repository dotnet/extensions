// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test.Options;

public class ExceptionPolicyOptionTest
{
    [Fact]
    public void CanConstruct()
    {
        var instance = new ExceptionPolicyOptions();
        Assert.NotNull(instance);
    }

    [Fact]
    public void CanSetAndGetEnabled()
    {
        var testValue = true;
        var instance = new ExceptionPolicyOptions
        {
            Enabled = testValue
        };
        Assert.Equal(testValue, instance.Enabled);
    }

    [Fact]
    public void CanSetAndGetInjectionRate()
    {
        var testValue = 0.7;
        var instance = new ExceptionPolicyOptions
        {
            FaultInjectionRate = testValue
        };
        Assert.Equal(testValue, instance.FaultInjectionRate);
    }

    [Fact]
    public void CanSetAndGetExceptionToInject()
    {
        var testValue = "SocketException";
        var instance = new ExceptionPolicyOptions
        {
            ExceptionKey = testValue
        };
        Assert.Equal(testValue, instance.ExceptionKey);
    }

    [Fact]
    public void InstanceHasDefaultValues()
    {
        var instance = new ExceptionPolicyOptions();
        Assert.Equal(ChaosPolicyOptionsBase.DefaultEnabled, instance.Enabled);
        Assert.Equal(ChaosPolicyOptionsBase.DefaultInjectionRate, instance.FaultInjectionRate);
        Assert.Equal(ExceptionPolicyOptions.DefaultExceptionKey, instance.ExceptionKey);
    }
}
