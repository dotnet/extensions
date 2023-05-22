// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test.Options;

public class LatencyPolicyOptionTest
{
    [Fact]
    public void CanConstruct()
    {
        var instance = new LatencyPolicyOptions();
        Assert.NotNull(instance);
    }

    [Fact]
    public void CanSetAndGetEnabled()
    {
        const bool TestValue = true;
        var instance = new LatencyPolicyOptions
        {
            Enabled = TestValue
        };
        Assert.Equal(TestValue, instance.Enabled);
    }

    [Fact]
    public void CanSetAndGetInjectionRate()
    {
        const double TestValue = 0.7;
        var instance = new LatencyPolicyOptions
        {
            FaultInjectionRate = TestValue
        };
        Assert.Equal(TestValue, instance.FaultInjectionRate);
    }

    [Fact]
    public void CanSetAndGetLatency()
    {
        var testValue = TimeSpan.FromSeconds(40);
        var instance = new LatencyPolicyOptions
        {
            Latency = testValue
        };
        Assert.Equal(testValue, instance.Latency);
    }

    [Fact]
    public void InstanceHasDefaultValues()
    {
        var instance = new LatencyPolicyOptions();
        Assert.Equal(ChaosPolicyOptionsBase.DefaultEnabled, instance.Enabled);
        Assert.Equal(ChaosPolicyOptionsBase.DefaultInjectionRate, instance.FaultInjectionRate);
        Assert.Equal(LatencyPolicyOptions.DefaultLatency, instance.Latency);
    }
}
