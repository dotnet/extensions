// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;
using Moq;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Enrichment.Service.Test;

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

    [Fact]
    public void ServiceTraceEnricher_GivenAnyNullArgument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddServiceTraceEnricher());

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddServiceTraceEnricher(_ => { }));

        Assert.Throws<ArgumentNullException>(() =>
            ((TracerProviderBuilder)null!).AddServiceTraceEnricher(Mock.Of<IConfigurationSection>()));

        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddOpenTelemetry().WithTracing(builder =>
                builder.AddServiceTraceEnricher((Action<ServiceTraceEnricherOptions>)null!)));

        Assert.Throws<ArgumentNullException>(() =>
            services.AddOpenTelemetry().WithTracing(builder =>
                builder.AddServiceTraceEnricher((IConfigurationSection)null!)));
    }

    [Fact]
    public void ServiceTraceEnricher_GivenNoArguments_RegistersInDI()
    {
        // Arrange & Act
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services =>
                services.AddOpenTelemetry().WithTracing(builder =>
                    builder.AddServiceTraceEnricher()))
            .Build();

        // Assert
        Assert.NotNull(host.Services.GetRequiredService<ITraceEnricher>());
        var options = host.Services.GetRequiredService<IOptions<ServiceTraceEnricherOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.Value);
        Assert.True(options.Value.ApplicationName);
        Assert.True(options.Value.EnvironmentName);
        Assert.False(options.Value.BuildVersion);
        Assert.False(options.Value.DeploymentRing);
    }

    [Fact]
    public void ServiceTraceEnricher_GivenOptions_RegistersInDI()
    {
        // Arrange & Act
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddServiceTraceEnricher(e =>
                    {
                        e.ApplicationName = false;
                        e.EnvironmentName = false;
                        e.BuildVersion = false;
                        e.DeploymentRing = false;
                    })))
            .Build();

        // Assert
        Assert.NotNull(host.Services.GetRequiredService<ITraceEnricher>());
        var options = host.Services.GetRequiredService<IOptions<ServiceTraceEnricherOptions>>().Value;
        Assert.NotNull(options);
        Assert.False(options.ApplicationName);
        Assert.False(options.EnvironmentName);
        Assert.False(options.BuildVersion);
        Assert.False(options.DeploymentRing);
    }

    [Fact]
    public void ServiceTraceEnricher_GivenConfiguration_RegistersInDI()
    {
        // Arrange & Act
        using var host = FakeHost.CreateBuilder()
            .ConfigureAppConfiguration(
                ("Serviceenrichersection:ApplicationName", "true"),
                ("Serviceenrichersection:EnvironmentName", "false"),
                ("Serviceenrichersection:BuildVersion", "true"),
                ("Serviceenrichersection:DeploymentRing", "true"))
            .ConfigureServices((context, services) => services
                .AddOpenTelemetry().WithTracing(builder => builder
                    .AddServiceTraceEnricher(context.Configuration.GetSection("Serviceenrichersection"))))
            .Build();

        // Assert
        var enricher = host.Services.GetRequiredService<ITraceEnricher>();
        Assert.NotNull(enricher);
        Assert.IsType<ServiceTraceEnricher>(enricher);
        var options = host.Services.GetRequiredService<IOptions<ServiceTraceEnricherOptions>>().Value;
        Assert.NotNull(options);
        Assert.True(options.ApplicationName);
        Assert.False(options.EnvironmentName);
        Assert.True(options.BuildVersion);
        Assert.True(options.DeploymentRing);
    }
}
