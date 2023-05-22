// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Test;

public class HttpContentOptionsRegistryTest
{
    [Fact]
    public void GetHttpContent_NullKey_ReturnNullResult()
    {
        var services = new ServiceCollection();
        services.AddHttpClientFaultInjection();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IHttpContentOptionsRegistry>();

        var result = registry.GetHttpContent(null!);
        Assert.Null(result);
    }

    [Fact]
    public void GetHttpContent_RegisteredKey_ShouldReturnInstance()
    {
        var testKey = "TestKey";
        using var testHttpContent = new StringContent("Test Content");
        var services = new ServiceCollection();
        services.AddHttpClientFaultInjection();

        var faultInjectionOptionsBuilder = new HttpFaultInjectionOptionsBuilder(services);
        faultInjectionOptionsBuilder.AddHttpContent(testKey, testHttpContent);

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IHttpContentOptionsRegistry>();

        var result = registry.GetHttpContent(testKey);
        Assert.Equal(testHttpContent, result);
    }

    [Fact]
    public void GetException_UnregisteredKey_ShouldReturnNull()
    {
        var services = new ServiceCollection();
        services.AddHttpClientFaultInjection();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IHttpContentOptionsRegistry>();

        var result = registry.GetHttpContent("testingtesting");
        Assert.Null(result);
    }
}
