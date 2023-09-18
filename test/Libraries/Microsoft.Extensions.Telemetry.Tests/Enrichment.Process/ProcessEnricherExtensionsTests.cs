// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Enrichment.Process.Test;

public class ProcessEnricherExtensionsTests
{
    [Fact]
    public void ProcessLogEnricher_GivenAnyNullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddProcessLogEnricher());

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddProcessLogEnricher(_ => { }));

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddProcessLogEnricher(Mock.Of<IConfigurationSection>()));

        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddProcessLogEnricher((IConfigurationSection)null!));

        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddProcessLogEnricher((Action<ProcessLogEnricherOptions>)null!));
    }

    [Fact]
    public void ProcessLogEnricher_GivenNoArguments_RegistersInDI()
    {
        // Arrange & Act
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services.AddProcessLogEnricher())
            .Build();

        // Assert
        Assert.NotNull(host.Services.GetRequiredService<ILogEnricher>());
    }

    [Fact]
    public void ProcessLogEnricher_GivenOptions_RegistersInDI()
    {
        // Arrange & Act
        using var host = FakeHost.CreateBuilder()
            .ConfigureLogging(builder => builder
                .Services.AddProcessLogEnricher(options =>
                {
                    options.ProcessId = false;
                    options.ThreadId = false;
                }))
            .Build();

        // Assert
        Assert.NotNull(host.Services.GetRequiredService<ILogEnricher>());
        var options = host.Services.GetRequiredService<IOptions<ProcessLogEnricherOptions>>().Value;
        Assert.NotNull(options);
        Assert.False(options.ProcessId);
        Assert.False(options.ThreadId);
    }

    [Fact]
    public void ProcessLogEnricher_GivenConfiguration_RegistersInDI()
    {
        // Arrange & Act
        const string TestSectionName = "processenrichersection";
        using var host = FakeHost.CreateBuilder()
            .ConfigureAppConfiguration(
                ($"{TestSectionName}:{nameof(ProcessLogEnricherOptions.ProcessId)}", "true"),
                ($"{TestSectionName}:{nameof(ProcessLogEnricherOptions.ThreadId)}", "false"))
            .ConfigureServices((context, services) => services
                .AddProcessLogEnricher(context.Configuration.GetSection(TestSectionName)))
            .Build();

        // Assert
        var enricher = host.Services.GetRequiredService<ILogEnricher>();
        Assert.NotNull(enricher);
        Assert.IsType<ProcessLogEnricher>(enricher);
        var options = host.Services.GetRequiredService<IOptions<ProcessLogEnricherOptions>>().Value;
        Assert.NotNull(options);
        Assert.True(options.ProcessId);
        Assert.False(options.ThreadId);
    }
}
