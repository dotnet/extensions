// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test.Internals;

public class FaultInjectionOptionsProviderTest
{
    [Fact]
    public void CanConstruct()
    {
        var options = new FaultInjectionOptions();
        var optionsMonitor = Mock.Of<IOptionsMonitor<FaultInjectionOptions>>(_ => _.CurrentValue == options);

        var chaosPolicyConfigProviderWithTelemetry = new FaultInjectionOptionsProvider(optionsMonitor);
        Assert.NotNull(chaosPolicyConfigProviderWithTelemetry);
    }

    [Fact]
    public void Constructor_ReloadOnChangeTrue_OptionsAreUpdated()
    {
        var builder = new ConfigurationBuilder().AddJsonFile("configs/optionsOnChangeTestOriginal.json", false, true);
        var config = builder.Build();

        var services = new ServiceCollection();
        services
            .AddFaultInjection(config.GetSection("ChaosPolicyConfigurations"));

        using var serviceProvider = services.BuildServiceProvider();
        var configurationProvider = serviceProvider.GetRequiredService<IFaultInjectionOptionsProvider>();

        // Trigger onChange callback of the options monitor
        var original = File.ReadAllText("configs/optionsOnChangeTestOriginal.json");
        File.Copy("configs/optionsOnChangeTestNew.json", "configs/optionsOnChangeTestOriginal.json", true);

        // Wait for 5 seconds
        Thread.Sleep(5000);

        // Verify that options is updated
        var result = configurationProvider.TryGetChaosPolicyOptionsGroup("OptionsGroupTest", out var optionsGroup);
        Assert.True(result);
        Assert.False(optionsGroup!.LatencyPolicyOptions!.Enabled);

        // Test clean up
        File.WriteAllText("configs/optionsOnChangeTestOriginal.json", original);
    }

    [Fact]
    public void GetChaosPolicyOptionsGroup_ValidOptionsGroupName_ShouldReturnInstance()
    {
        var testOptionsGroup = new ChaosPolicyOptionsGroup();
        var testOptionsGroupName = "TestOptionsGroup";
        var options = new FaultInjectionOptions
        {
            ChaosPolicyOptionsGroups = new Dictionary<string, ChaosPolicyOptionsGroup>
                {
                    { testOptionsGroupName, testOptionsGroup }
                }
        };

        var optionsMonitor = Mock.Of<IOptionsMonitor<FaultInjectionOptions>>(_ => _.CurrentValue == options);
        var chaosPolicyConfigProvider = new FaultInjectionOptionsProvider(optionsMonitor);

        var result = chaosPolicyConfigProvider.TryGetChaosPolicyOptionsGroup(testOptionsGroupName, out var optionsGroup);
        Assert.True(result);
        Assert.Equal(optionsGroup, testOptionsGroup);
    }

    [Fact]
    public void GetChaosPolicyOptionsGroup_InvalidOptionsGroupName_ShouldReturnNull()
    {
        var options = new FaultInjectionOptions();
        var optionsMonitor = Mock.Of<IOptionsMonitor<FaultInjectionOptions>>(_ => _.CurrentValue == options);
        var chaosPolicyConfigProvider = new FaultInjectionOptionsProvider(optionsMonitor);

        var result = chaosPolicyConfigProvider.TryGetChaosPolicyOptionsGroup("RandomName", out var optionsGroup);
        Assert.False(result);
        Assert.Null(optionsGroup);
    }

    [Fact]
    public void GetChaosPolicyOptionsGroup_NullOptionsGroupName_ShouldThrow()
    {
        var options = new FaultInjectionOptions();
        var optionsMonitor = Mock.Of<IOptionsMonitor<FaultInjectionOptions>>(_ => _.CurrentValue == options);
        var chaosPolicyConfigProvider = new FaultInjectionOptionsProvider(optionsMonitor);

        string? optionsGroupName = null;
        Assert.Throws<ArgumentNullException>(() => chaosPolicyConfigProvider.TryGetChaosPolicyOptionsGroup(optionsGroupName!, out var optionsGroup));
    }
}
