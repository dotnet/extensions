// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test.Internals;

public class CustomResultRegistryTest
{
    [Fact]
    public void GetCustomResult_RegisteredKey_ShouldReturnInstance()
    {
        var testCustomResultKey = "TestCustomResultKey";
        var testCustomResultInstance = "Test custom result";
        var services = new ServiceCollection();
        services.AddFaultInjection();

        var faultInjectionOptionsBuilder = new FaultInjectionOptionsBuilder(services);
        faultInjectionOptionsBuilder.AddCustomResult(testCustomResultKey, testCustomResultInstance);

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ICustomResultRegistry>();

        var result = registry.GetCustomResult(testCustomResultKey);
        Assert.Equal(testCustomResultInstance, result);
    }

    [Fact]
    public void GetCustomResult_UnregisteredKey_ShouldReturnDefaultInstance()
    {
        var services = new ServiceCollection();
        services.AddFaultInjection();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ICustomResultRegistry>();

        var result = registry.GetCustomResult("testingtesting");
        Assert.IsAssignableFrom<object>(result);
    }
}
