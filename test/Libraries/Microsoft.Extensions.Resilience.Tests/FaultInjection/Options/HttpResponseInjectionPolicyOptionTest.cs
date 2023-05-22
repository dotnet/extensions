// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test.Options;

public class HttpResponseInjectionPolicyOptionTest
{
    [Fact]
    public void CanConstruct()
    {
        var instance = new HttpResponseInjectionPolicyOptions();
        Assert.NotNull(instance);
    }

    [Fact]
    public void CanSetAndGetEnabled()
    {
        const bool TestValue = true;
        var instance = new HttpResponseInjectionPolicyOptions
        {
            Enabled = TestValue
        };
        Assert.Equal(TestValue, instance.Enabled);
    }

    [Fact]
    public void CanSetAndGetInjectionRate()
    {
        const double TestValue = 0.7;
        var instance = new HttpResponseInjectionPolicyOptions
        {
            FaultInjectionRate = TestValue
        };
        Assert.Equal(TestValue, instance.FaultInjectionRate);
    }

    [Fact]
    public void CanSetAndGetStatusCode()
    {
        const HttpStatusCode TestValue = HttpStatusCode.BadRequest;
        var instance = new HttpResponseInjectionPolicyOptions
        {
            StatusCode = TestValue
        };
        Assert.Equal(TestValue, instance.StatusCode);
    }

    [Fact]
    public void InstanceHasDefaultValues()
    {
        var instance = new HttpResponseInjectionPolicyOptions();
        Assert.Equal(ChaosPolicyOptionsBase.DefaultEnabled, instance.Enabled);
        Assert.Equal(ChaosPolicyOptionsBase.DefaultInjectionRate, instance.FaultInjectionRate);
        Assert.Equal(HttpResponseInjectionPolicyOptions.DefaultStatusCode, instance.StatusCode);
    }
}
