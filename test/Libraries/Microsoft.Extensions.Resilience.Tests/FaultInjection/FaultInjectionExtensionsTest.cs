// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test;

public class FaultInjectionExtensionsTest
{
    private readonly IConfiguration _configurationWithPolicyOptions;

    public FaultInjectionExtensionsTest()
    {
        var builder = new ConfigurationBuilder().AddJsonFile("configs/appsettings.json");
        _configurationWithPolicyOptions = builder.Build();
    }

    [Fact]
    public void AddFaultInjection_ShouldRegisterRequiredServices()
    {
        var services = new ServiceCollection();
        services
            .AddLogging()
            .RegisterMetrics()
            .AddFaultInjection();

        using var serviceProvider = services.BuildServiceProvider();

        var chaosPolicyConfigProvider = serviceProvider.GetService<IFaultInjectionOptionsProvider>();
        Assert.IsAssignableFrom<IFaultInjectionOptionsProvider>(chaosPolicyConfigProvider);

        var exceptionRegistry = serviceProvider.GetService<IExceptionRegistry>();
        Assert.IsAssignableFrom<IExceptionRegistry>(exceptionRegistry);

        var policyFactory = serviceProvider.GetService<IChaosPolicyFactory>();
        Assert.IsAssignableFrom<IChaosPolicyFactory>(policyFactory);
    }

    [Fact]
    public void AddFaultInjection_NullServices_ShouldThrow()
    {
        ServiceCollection? services = null!;
        Assert.Throws<ArgumentNullException>(() => services.AddFaultInjection());
    }

    [Fact]
    public void AddFaultInjection_WithConfigurationSection_ShouldRegisterRequiredServices()
    {
        var services = new ServiceCollection();
        services
            .AddLogging()
            .RegisterMetrics()
            .AddFaultInjection(_configurationWithPolicyOptions.GetSection("ChaosPolicyConfigurations"));

        using var serviceProvider = services.BuildServiceProvider();

        var chaosPolicyConfigProvider = serviceProvider.GetService<IFaultInjectionOptionsProvider>();
        Assert.IsAssignableFrom<IFaultInjectionOptionsProvider>(chaosPolicyConfigProvider);

        var exceptionRegistry = serviceProvider.GetService<IExceptionRegistry>();
        Assert.IsAssignableFrom<IExceptionRegistry>(exceptionRegistry);

        var policyFactory = serviceProvider.GetService<IChaosPolicyFactory>();
        Assert.IsAssignableFrom<IChaosPolicyFactory>(policyFactory);
    }

    [Fact]
    public void AddFaultInjection_WithConfigurationSection_NullServices_ShouldThrow()
    {
        ServiceCollection? services = null!;

        Assert.Throws<ArgumentNullException>(
            () => services.AddFaultInjection(_configurationWithPolicyOptions.GetSection("ChaosPolicyConfigurations")));
    }

    [Fact]
    public void AddFaultInjection_NullConfigurationSection_ShouldThrow()
    {
        var services = new ServiceCollection();
        IConfigurationSection? configurationSection = null!;

        Assert.Throws<ArgumentNullException>(
            () => services.AddFaultInjection(configurationSection));
    }

    [Fact]
    public void AddFaultInjection_WithAction_ShouldInvokeActionAndRegisterRequiredServices()
    {
        var testExceptionKey1 = "TestException1";
        var testExceptionKey2 = "TestException2";
        var testException1 = new InjectedFaultException("TestException1");
        var testException2 = new InjectedFaultException("TestException2");

        var services = new ServiceCollection();
        services
            .AddLogging()
            .RegisterMetrics()
            .AddFaultInjection(builder =>
                builder
                    .Configure(_configurationWithPolicyOptions.GetSection("ChaosPolicyConfigurations"))
                    .AddException(testExceptionKey1, testException1)
                    .AddException(testExceptionKey2, testException2));

        using var serviceProvider = services.BuildServiceProvider();

        var chaosPolicyConfigProviderOptions = serviceProvider.GetRequiredService<IOptions<FaultInjectionOptions>>().Value;
        Assert.IsAssignableFrom<FaultInjectionOptions>(chaosPolicyConfigProviderOptions);
        Assert.NotNull(chaosPolicyConfigProviderOptions.ChaosPolicyOptionsGroups["OptionsGroupTest"]);

        var faultInjectionExceptionOptions1 = serviceProvider.GetRequiredService<IOptionsMonitor<FaultInjectionExceptionOptions>>().Get(testExceptionKey1);
        Assert.IsAssignableFrom<FaultInjectionExceptionOptions>(faultInjectionExceptionOptions1);
        Assert.Equal(testException1, faultInjectionExceptionOptions1.Exception);

        var faultInjectionExceptionOptions2 = serviceProvider.GetRequiredService<IOptionsMonitor<FaultInjectionExceptionOptions>>().Get(testExceptionKey2);
        Assert.IsAssignableFrom<FaultInjectionExceptionOptions>(faultInjectionExceptionOptions2);
        Assert.Equal(testException2, faultInjectionExceptionOptions2.Exception);

        var chaosPolicyConfigProvider = serviceProvider.GetRequiredService<IFaultInjectionOptionsProvider>();
        Assert.IsAssignableFrom<IFaultInjectionOptionsProvider>(chaosPolicyConfigProvider);

        var exceptionRegistry = serviceProvider.GetRequiredService<IExceptionRegistry>();
        Assert.IsAssignableFrom<IExceptionRegistry>(exceptionRegistry);

        var policyFactory = serviceProvider.GetService<IChaosPolicyFactory>();
        Assert.IsAssignableFrom<IChaosPolicyFactory>(policyFactory);
    }

    [Fact]
    public void AddFaultInjection_NullAction_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action<FaultInjectionOptionsBuilder>? action = null!;
        Assert.Throws<ArgumentNullException>(() => services.AddFaultInjection(action));
    }

    [Fact]
    public void AddFaultInjection_WithAction_NullServices_ShouldThrow()
    {
        ServiceCollection? services = null!;

        Assert.Throws<ArgumentNullException>(
            () => services.AddFaultInjection(builder => { }));
    }

    [Fact]
    public void WithFaultInjection_NullContext_ShouldThrow()
    {
        Context? context = null!;
        Assert.Throws<ArgumentNullException>(() => context.WithFaultInjection("Test"));
    }

    [Fact]
    public void WithFaultInjection_NullGroupName_ShouldThrow()
    {
        var context = new Context();
        string? testGroupName = null!;

        Assert.Throws<ArgumentNullException>(() => context.WithFaultInjection(testGroupName));
    }

    [Fact]
    public void WithFaultInjection_GetFaultInjectionGroupName_IfRegistered_ShouldReturnGroupName()
    {
        var context = new Context();
        var groupName = "test";
        context.WithFaultInjection(groupName);

        var result = context.GetFaultInjectionGroupName();
        Assert.Equal(groupName, result);
    }

    [Fact]
    public void GetFaultInjectionGroupName_NullContext_ShouldThrow()
    {
        Context? context = null!;
        Assert.Throws<ArgumentNullException>(() => context.GetFaultInjectionGroupName());
    }

    [Fact]
    public void GetFaultInjectionGroupName_IfNotRegistered_ShouldReturnNull()
    {
        var context = new Context();
        var result = context.GetFaultInjectionGroupName();
        Assert.Null(result);
    }

    [Fact]
    public void GetFaultInjectionGroupName_EmptyWeightedAssignments_ShouldReturnNull()
    {
        var context = new Context();
        var weightAssignments = new FaultPolicyWeightAssignmentsOptions();
        context.WithFaultInjection(weightAssignments);

        var result = context.GetFaultInjectionGroupName();
        Assert.Null(result);
    }

    [Fact]
    public void WithFaultInjection_WeightedAssignments()
    {
        var context = new Context();
        var weightAssignments = new FaultPolicyWeightAssignmentsOptions();

        weightAssignments.WeightAssignments.Add("TestA", 40);
        weightAssignments.WeightAssignments.Add("TestB", 20);
        weightAssignments.WeightAssignments.Add("TestC", 30);
        weightAssignments.WeightAssignments.Add("TestD", 10);
        context.WithFaultInjection(weightAssignments);

        // Check the ordering
        var contextWeightAssignments = (Dictionary<string, double>)context["ChaosPolicyOptionsGroupName"];
        var prev = 0.0;
        foreach (var entry in contextWeightAssignments)
        {
            Assert.True(prev < entry.Value);
            prev = entry.Value;
        }

        for (int i = 0; i < 100; i++)
        {
            var result = context.GetFaultInjectionGroupName();
            Assert.True(result == "TestA" || result == "TestB" || result == "TestC" || result == "TestD");
        }
    }

    [Fact]
    public void WithFaultInjection_WeightedAssignments_NullContext_ShouldThrow()
    {
        Context? context = null!;
        var weightAssignments = new FaultPolicyWeightAssignmentsOptions();

        Assert.Throws<ArgumentNullException>(() => context.WithFaultInjection(weightAssignments));
    }

    [Fact]
    public void WithFaultInjection_NullWeightedAssignments_ShouldThrow()
    {
        var context = new Context();
        FaultPolicyWeightAssignmentsOptions? weightAssignments = null!;

        Assert.Throws<ArgumentNullException>(() => context.WithFaultInjection(weightAssignments));
    }

    [Fact]
    public void WithFaultInjection_WeightedAssignments_Mutation_Check()
    {
        var context = new Context();
        var weightAssignments = new FaultPolicyWeightAssignmentsOptions();

        weightAssignments.WeightAssignments.Add("TestA", 10);
        context.WithFaultInjection(weightAssignments);

        for (int i = 0; i < 100; i++)
        {
            var result = context.GetFaultInjectionGroupName();
            Assert.True(result == "TestA");
        }
    }
}
