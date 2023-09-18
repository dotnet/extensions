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

namespace Microsoft.Extensions.Diagnostics.Enrichment.Service.Test;

public class ServiceEnricherExtensionsTests
{
    [Fact]
    public void ServiceLogEnricher_GivenAnyNullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddServiceLogEnricher());

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddServiceLogEnricher(_ => { }));

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddServiceLogEnricher(Mock.Of<IConfigurationSection>()));

        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddServiceLogEnricher((IConfigurationSection)null!));
    }

    [Fact]
    public void ServiceLogEnricher_GivenNoArguments_RegistersInDI()
    {
        // Arrange & Act
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services.AddServiceLogEnricher())
            .Build();

        // Assert
        Assert.NotNull(host.Services.GetRequiredService<IStaticLogEnricher>());
    }

    [Fact]
    public void HostLogEnricher_GivenOptions_RegistersInDI()
    {
        // Arrange & Act
        using var host = FakeHost.CreateBuilder()
            .ConfigureLogging(builder => builder
                .Services.AddServiceLogEnricher(e =>
                {
                    e.ApplicationName = false;
                    e.EnvironmentName = false;
                    e.BuildVersion = false;
                    e.DeploymentRing = false;
                }))
            .Build();

        // Assert
        Assert.NotNull(host.Services.GetRequiredService<IStaticLogEnricher>());
        var options = host.Services.GetRequiredService<IOptions<ServiceLogEnricherOptions>>().Value;
        Assert.NotNull(options);
        Assert.False(options.ApplicationName);
        Assert.False(options.EnvironmentName);
        Assert.False(options.BuildVersion);
        Assert.False(options.DeploymentRing);
    }

    [Fact]
    public void ServiceLogEnricher_GivenConfiguration_RegistersInDI()
    {
        // Arrange & Act
        using var host = FakeHost.CreateBuilder()
            .ConfigureAppConfiguration(
                ("Serviceenrichersection:ApplicationName", "true"),
                ("Serviceenrichersection:EnvironmentName", "false"),
                ("Serviceenrichersection:BuildVersion", "true"),
                ("Serviceenrichersection:DeploymentRing", "true"))
            .ConfigureServices((context, services) => services
                .AddServiceLogEnricher(context.Configuration.GetSection("Serviceenrichersection")))
            .Build();

        // Assert
        var enricher = host.Services.GetRequiredService<IStaticLogEnricher>();
        Assert.NotNull(enricher);
        Assert.IsType<ServiceLogEnricher>(enricher);
        var options = host.Services.GetRequiredService<IOptions<ServiceLogEnricherOptions>>().Value;
        Assert.NotNull(options);
        Assert.True(options.ApplicationName);
        Assert.False(options.EnvironmentName);
        Assert.True(options.BuildVersion);
        Assert.True(options.DeploymentRing);
    }
}
