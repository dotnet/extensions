// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test.Options;

public class ChaosPolicyOptionsGroupTest
{
    [Fact]
    public void CanConstruct()
    {
        var optionsGroup = new ChaosPolicyOptionsGroup();
        Assert.NotNull(optionsGroup);
    }

    [Fact]
    public void CanSetAndGetLatencyPolicyOptions()
    {
        var testLatencyOptions = new LatencyPolicyOptions();
        var optionsGroup = new ChaosPolicyOptionsGroup
        {
            LatencyPolicyOptions = testLatencyOptions
        };
        Assert.Equal(optionsGroup.LatencyPolicyOptions, testLatencyOptions);
    }

    [Fact]
    public void CanSetAndGetHttpResponseInjectionPolicyOptions()
    {
        var testHttpOptions = new HttpResponseInjectionPolicyOptions();
        var optionsGroup = new ChaosPolicyOptionsGroup
        {
            HttpResponseInjectionPolicyOptions = testHttpOptions
        };
        Assert.Equal(optionsGroup.HttpResponseInjectionPolicyOptions, testHttpOptions);
    }

    [Fact]
    public void CanSetAndGetExceptionPolicyOptions()
    {
        var testExceptionOptions = new ExceptionPolicyOptions();
        var optionsGroup = new ChaosPolicyOptionsGroup
        {
            ExceptionPolicyOptions = testExceptionOptions
        };
        Assert.Equal(optionsGroup.ExceptionPolicyOptions, testExceptionOptions);
    }
}
