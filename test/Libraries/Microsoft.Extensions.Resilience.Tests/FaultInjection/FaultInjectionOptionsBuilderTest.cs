// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test;

public class FaultInjectionOptionsBuilderTest
{
    [Fact]
    public void CanConstruct()
    {
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new FaultInjectionOptionsBuilder(services);

        Assert.NotNull(faultInjectionOptionsBuilder);
    }

    [Fact]
    public void Constructor_NullServices_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new FaultInjectionOptionsBuilder(null!));
    }

    [Fact]
    public void Configure_WithConfigurationSection_ShouldConfigureChaosPolicyConfigProviderOptions()
    {
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new FaultInjectionOptionsBuilder(services);

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
        var faultInjectionOptionsBuilder = new FaultInjectionOptionsBuilder(services);

        Assert.Throws<ArgumentNullException>(
            () => faultInjectionOptionsBuilder.Configure((IConfigurationSection)null!));
    }

    [Fact]
    public void Configure_WithAction_ShouldConfigureChaosPolicyConfigProviderOptions()
    {
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new FaultInjectionOptionsBuilder(services);
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
        var faultInjectionOptionsBuilder = new FaultInjectionOptionsBuilder(services);

        Assert.Throws<ArgumentNullException>(
            () => faultInjectionOptionsBuilder.Configure((Action<FaultInjectionOptions>)null!));
    }

    [Fact]
    public void AddExceptionForFaultInjection_ShouldAddInstanceToExceptionRegistryOptions()
    {
        var testExceptionKey = "TestExceptionKey";
        var testExceptionInstance = new InjectedFaultException();
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new FaultInjectionOptionsBuilder(services);
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
        var faultInjectionOptionsBuilder = new FaultInjectionOptionsBuilder(services);

        Assert.Throws<ArgumentNullException>(() =>
            faultInjectionOptionsBuilder.AddException(testExceptionKey, testExceptionInstance));
    }

    [Fact]
    public void AddExceptionForFaultInjection_KeyNullOrWhiteSpace_ShouldThrow()
    {
        string? testExceptionKey = null!;
        var testExceptionInstance = new InjectedFaultException();
        var services = new ServiceCollection();
        var faultInjectionOptionsBuilder = new FaultInjectionOptionsBuilder(services);

        Assert.Throws<ArgumentNullException>(() =>
            faultInjectionOptionsBuilder.AddException(testExceptionKey, testExceptionInstance));

        testExceptionKey = "";
        Assert.Throws<ArgumentException>(() =>
            faultInjectionOptionsBuilder.AddException(testExceptionKey, testExceptionInstance));
    }
}
