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

namespace Microsoft.Extensions.Diagnostics.Enrichment.Test;

public class ApplicationEnricherExtensionsTests
{
    [Fact]
    public void ServiceLogEnricher_GivenAnyNullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddApplicationLogEnricher());

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddApplicationLogEnricher(_ => { }));

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddApplicationLogEnricher(Mock.Of<IConfigurationSection>()));

        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddApplicationLogEnricher((IConfigurationSection)null!));
    }

    [Fact]
    public void ServiceLogEnricher_GivenNoArguments_RegistersInDI()
    {
        // Arrange & Act
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services.AddApplicationLogEnricher())
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
                .Services.AddApplicationLogEnricher(e =>
                {
                    e.ApplicationName = false;
                    e.EnvironmentName = false;
                    e.BuildVersion = false;
                    e.DeploymentRing = false;
                }))
            .Build();

        // Assert
        Assert.NotNull(host.Services.GetRequiredService<IStaticLogEnricher>());
        var options = host.Services.GetRequiredService<IOptions<ApplicationLogEnricherOptions>>().Value;
        Assert.NotNull(options);
        Assert.False(options.ApplicationName);
        Assert.False(options.EnvironmentName);
        Assert.False(options.BuildVersion);
        Assert.False(options.DeploymentRing);
    }

    [Fact]
    public void ApplicationLogEnricher_GivenConfiguration_RegistersInDI()
    {
        // Arrange & Act
        using var host = FakeHost.CreateBuilder()
            .ConfigureAppConfiguration(
                ("Applicationenrichersection:ApplicationName", "true"),
                ("Applicationenrichersection:EnvironmentName", "false"),
                ("Applicationenrichersection:BuildVersion", "true"),
                ("Applicationenrichersection:DeploymentRing", "true"))
            .ConfigureServices((context, services) => services
                .AddApplicationLogEnricher(context.Configuration.GetSection("Applicationenrichersection")))
            .Build();

        // Assert
        var enricher = host.Services.GetRequiredService<IStaticLogEnricher>();
        Assert.NotNull(enricher);
        Assert.IsType<ApplicationLogEnricher>(enricher);
        var options = host.Services.GetRequiredService<IOptions<ApplicationLogEnricherOptions>>().Value;
        Assert.NotNull(options);
        Assert.True(options.ApplicationName);
        Assert.False(options.EnvironmentName);
        Assert.True(options.BuildVersion);
        Assert.True(options.DeploymentRing);
    }
}
