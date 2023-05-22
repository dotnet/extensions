// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test.Internals;

public class ExceptionRegistryTest
{
    [Fact]
    public void GetException_NullKey_ShouldThrow()
    {
        var services = new ServiceCollection();
        services.AddFaultInjection();

        using var provider = services.BuildServiceProvider();
        var exceptionRegistry = provider.GetRequiredService<IExceptionRegistry>();

        Assert.Throws<ArgumentNullException>(() => exceptionRegistry.GetException(null!));
    }

    [Fact]
    public void GetException_RegisteredKey_ShouldReturnInstance()
    {
        var testExceptionKey = "TestExceptionKey";
        var testExceptionInstance = new InjectedFaultException();
        var services = new ServiceCollection();
        services.AddFaultInjection();

        var faultInjectionOptionsBuilder = new FaultInjectionOptionsBuilder(services);
        faultInjectionOptionsBuilder.AddException(testExceptionKey, testExceptionInstance);

        using var provider = services.BuildServiceProvider();
        var exceptionRegistry = provider.GetRequiredService<IExceptionRegistry>();

        var result = exceptionRegistry.GetException(testExceptionKey);
        Assert.Equal(testExceptionInstance, result);
    }

    [Fact]
    public void GetException_UnregisteredKey_ShouldReturnDefaultInstance()
    {
        var services = new ServiceCollection();
        services.AddFaultInjection();

        using var provider = services.BuildServiceProvider();
        var exceptionRegistry = provider.GetRequiredService<IExceptionRegistry>();

        var result = exceptionRegistry.GetException("testingtesting");
        Assert.IsAssignableFrom<InjectedFaultException>(result);
    }
}
