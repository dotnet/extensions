// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.FaultInjection;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Test;

public class HttpFaultInjectionOptionsBuilderTest
{
    [Fact]
    public void CanConstruct()
    {
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new HttpFaultInjectionOptionsBuilder(services);

        Assert.NotNull(faultInjectionOptionsBuilder);
    }

    [Fact]
    public void Constructor_NullServices_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new HttpFaultInjectionOptionsBuilder(null!));
    }

    [Fact]
    public void Configure_WithConfigurationSection_ShouldConfigureChaosPolicyConfigProviderOptions()
    {
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new HttpFaultInjectionOptionsBuilder(services);

        var builder = new ConfigurationBuilder().AddJsonFile("configs/appsettings.json");
        var configuration = builder.Build();

        faultInjectionOptionsBuilder.Configure(configuration.GetSection("ChaosPolicyConfigurations"));

        using var provider = services.BuildServiceProvider();
        var result = provider.GetRequiredService<IOptions<FaultInjectionOptions>>().Value;
        Assert.IsAssignableFrom<FaultInjectionOptions>(result);
        Assert.NotNull(result.ChaosPolicyOptionsGroups?["OptionsGroupTest"]);
    }

    [Fact]
    public void Configure_WithConfigurationSection_NullConfigurationSection_ShouldThrow()
    {
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new HttpFaultInjectionOptionsBuilder(services);

        Assert.Throws<ArgumentNullException>(
            () => faultInjectionOptionsBuilder.Configure((IConfigurationSection)null!));
    }

    [Fact]
    public void Configure_WithAction_ShouldConfigureChaosPolicyConfigProviderOptions()
    {
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new HttpFaultInjectionOptionsBuilder(services);
        var testChaosPolicyOptionsGroups = new Dictionary<string, ChaosPolicyOptionsGroup>();

        faultInjectionOptionsBuilder.Configure(options =>
        {
            options.ChaosPolicyOptionsGroups = testChaosPolicyOptionsGroups;
        });

        using var provider = services.BuildServiceProvider();
        var result = provider.GetRequiredService<IOptions<FaultInjectionOptions>>().Value;
        Assert.IsAssignableFrom<FaultInjectionOptions>(result);
        Assert.Equal(testChaosPolicyOptionsGroups, result.ChaosPolicyOptionsGroups);
    }

    [Fact]
    public void Configure_WithAction_NullAction_ShouldThrow()
    {
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new HttpFaultInjectionOptionsBuilder(services);

        Assert.Throws<ArgumentNullException>(
            () => faultInjectionOptionsBuilder.Configure((Action<FaultInjectionOptions>)null!));
    }

    [Fact]
    public void AddHttpContent_ShouldAddInstanceToHttpContentOptionsRegistry()
    {
        var testKey = "TestKey";
        using var testHttpContent = new StringContent("Test Content");
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new HttpFaultInjectionOptionsBuilder(services);
        faultInjectionOptionsBuilder.AddHttpContent(testKey, testHttpContent);

        using var provider = services.BuildServiceProvider();
        var httpContentOptions = provider.GetRequiredService<IOptionsMonitor<HttpContentOptions>>().Get(testKey);

        Assert.Equal(httpContentOptions.HttpContent, testHttpContent);
    }

    [Fact]
    public void AddHttpContent_NullExceptionInstance_ShouldThrow()
    {
        var testKey = "TestKey";
        HttpContent? testContent = null!;

        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new HttpFaultInjectionOptionsBuilder(services);

        Assert.Throws<ArgumentNullException>(() =>
            faultInjectionOptionsBuilder.AddHttpContent(testKey, testContent));
    }

    [Fact]
    public void AddHttpContent_KeyNullOrWhiteSpace_ShouldThrow()
    {
        string? testKey = null!;
        using var testHttpContent = new StringContent("Test Content");
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new HttpFaultInjectionOptionsBuilder(services);

        Assert.Throws<ArgumentNullException>(() =>
            faultInjectionOptionsBuilder.AddHttpContent(testKey, testHttpContent));

        testKey = "";
        Assert.Throws<ArgumentException>(() =>
            faultInjectionOptionsBuilder.AddHttpContent(testKey, testHttpContent));
    }

    [Fact]
    public void AddExceptionForFaultInjection_ShouldAddInstanceToExceptionRegistryOptions()
    {
        var testExceptionKey = "TestExceptionKey";
        var testExceptionInstance = new InjectedFaultException();
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new HttpFaultInjectionOptionsBuilder(services);
        faultInjectionOptionsBuilder.AddException(testExceptionKey, testExceptionInstance);

        using var provider = services.BuildServiceProvider();
        var faultInjectionExceptionOptions = provider.GetRequiredService<IOptionsMonitor<FaultInjectionExceptionOptions>>().Get(testExceptionKey);

        Assert.Equal(faultInjectionExceptionOptions.Exception, testExceptionInstance);
    }

    [Fact]
    public void AddExceptionForFaultInjection_NullExceptionInstance_ShouldThrow()
    {
        var testExceptionKey = "TestExceptionKey";
        Exception? testExceptionInstance = null!;

        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new HttpFaultInjectionOptionsBuilder(services);

        Assert.Throws<ArgumentNullException>(() =>
            faultInjectionOptionsBuilder.AddException(testExceptionKey, testExceptionInstance));
    }

    [Fact]
    public void AddExceptionForFaultInjection_KeyNullOrWhiteSpace_ShouldThrow()
    {
        string? testExceptionKey = null!;
        var testExceptionInstance = new InjectedFaultException();
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new HttpFaultInjectionOptionsBuilder(services);

        Assert.Throws<ArgumentNullException>(() =>
            faultInjectionOptionsBuilder.AddException(testExceptionKey, testExceptionInstance));

        testExceptionKey = "";
        Assert.Throws<ArgumentException>(() =>
            faultInjectionOptionsBuilder.AddException(testExceptionKey, testExceptionInstance));
    }
}
