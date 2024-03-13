// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP3_1_OR_GREATER
using Microsoft.AspNetCore.TestHost;
#else
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if NETCOREAPP3_1_OR_GREATER
using Microsoft.Extensions.Hosting.Testing;
#endif
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Probes.Test;

public class TcpEndpointProbesExtensionsTests
{
    [Fact]
    public void AddTcpEndpointProbeTest_WithoutConfig()
    {
        using var host = CreateWebHost(services =>
        {
            services
                .AddRouting()
                .AddTcpEndpointProbe();
        });

        var hostedServices = host.Services.GetServices<IHostedService>().Where(x => x is TcpEndpointProbesService);

        Assert.Single(hostedServices);
    }

    [Fact]
    public void AddTcpEndpointProbeTest_WithAction()
    {
        using var host = CreateWebHost(services =>
        {
            services
                .AddRouting()
                .AddTcpEndpointProbe(o =>
                {
                    o.FilterChecks = _ => false;
                    o.HealthAssessmentPeriod = TimeSpan.FromSeconds(15);
                });
        });

        var hostedServices = host.Services.GetServices<IHostedService>().Where(x => x is TcpEndpointProbesService);
        var configurations = host.Services.GetServices<IOptions<TcpEndpointProbesOptions>>();

        Assert.Single(hostedServices);
        var config = Assert.Single(configurations);
        Assert.Equal(TimeSpan.FromSeconds(15), config.Value.HealthAssessmentPeriod);
    }

    [Fact]
    public void AddTcpEndpointProbeTest_WithName_WithoutConfig()
    {
        using var host = CreateWebHost(services =>
        {
            services
                .AddRouting()
                .AddTcpEndpointProbe("Liveness");
        });

        var hostedServices = host.Services.GetServices<IHostedService>().Where(x => x is TcpEndpointProbesService);

        Assert.Single(hostedServices);
    }

    [Fact]
    public void AddTcpEndpointProbeTest_WithName_WithAction()
    {
        using var host = CreateWebHost(services =>
        {
            services
                .AddRouting()
                .AddTcpEndpointProbe("Liveness", o =>
                {
                    o.FilterChecks = _ => false;
                    o.HealthAssessmentPeriod = TimeSpan.FromSeconds(5);
                });
        });

        var hostedServices = host.Services.GetServices<IHostedService>().Where(x => x is TcpEndpointProbesService);
        var configurations = host.Services.GetServices<IOptionsMonitor<TcpEndpointProbesOptions>>();

        Assert.Single(hostedServices);
        var config = Assert.Single(configurations);
        Assert.NotNull(config.Get("Liveness"));
        Assert.Equal(TimeSpan.FromSeconds(5), config.Get("Liveness").HealthAssessmentPeriod);
    }

    [Fact]
    public void AddTcpEndpointProbeTest_WithConfigurationSection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TcpHealthCheck:TcpPort"] = "1234",
            })
            .Build();

        using var host = CreateWebHost(services =>
        {
            services
                .AddRouting()
                .AddTcpEndpointProbe(config.GetSection("TcpHealthCheck"));
        });

        var hostedServices = host.Services.GetServices<IHostedService>().Where(x => x is TcpEndpointProbesService);
        var configurations = host.Services.GetServices<IOptions<TcpEndpointProbesOptions>>();

        Assert.Single(hostedServices);
        var configuration = Assert.Single(configurations);
        Assert.Equal(1234, configuration.Value.TcpPort);
    }

    [Fact]
    public void AddTcpEndpointProbeTest_WithName_WithConfigurationSection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TcpHealthCheck:TcpPort"] = "1234",
            })
            .Build();

        using var host = CreateWebHost(services =>
        {
            services
                .AddRouting()
                .AddTcpEndpointProbe("Liveness", config.GetSection("TcpHealthCheck"));
        });

        var hostedServices = host.Services.GetServices<IHostedService>().Where(x => x is TcpEndpointProbesService);
        var configurations = host.Services.GetServices<IOptionsMonitor<TcpEndpointProbesOptions>>();

        Assert.Single(hostedServices);
        Assert.Single(configurations);
        var configuration = configurations.First();
        Assert.NotNull(configuration.Get("Liveness"));
        Assert.Equal(1234, configuration.Get("Liveness").TcpPort);
    }

    [Fact]
    public void AddTcpEndpointProbeTest_MultipleNamed()
    {
        using var host = CreateWebHost(services =>
        {
            services
                .AddRouting()
                .AddTcpEndpointProbe("Liveness")
                .AddTcpEndpointProbe("Readiness");
        });

        var hostedServices = host.Services.GetServices<IHostedService>().Where(x => x is TcpEndpointProbesService);
        var configurations = host.Services.GetServices<IOptionsMonitor<TcpEndpointProbesOptions>>();

        Assert.Equal(2, hostedServices.Count());
        Assert.Single(configurations);
        var config = configurations.First();
        Assert.NotNull(config.Get("Liveness"));
        Assert.NotNull(config.Get("Readiness"));
    }

#if NETCOREAPP3_1_OR_GREATER
    private static IHost CreateWebHost(Action<IServiceCollection> configureServices)
    {
        return FakeHost.CreateBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(configureServices))
            .Build();
    }
#else
    private static IWebHost CreateWebHost(Action<IServiceCollection> configureServices)
    {
        return new WebHostBuilder()
            .ConfigureServices(configureServices)
            .Configure(app => app
                .UseRouter(routes => { })
                .UseMvc())
            .Build();
    }
#endif
}
