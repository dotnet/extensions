// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
#if NETCOREAPP3_1_OR_GREATER
using Microsoft.AspNetCore.TestHost;
#else
using Microsoft.AspNetCore.Builder;
#endif
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
#if NETCOREAPP3_1_OR_GREATER
using Microsoft.Extensions.Hosting.Testing;
#endif
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Probes.Test;

public class KubernetesProbesExtensionsTests
{
    [Fact]
    public void AddKubernetesProbes_RegistersAllProbes()
    {
        using var host = CreateWebHost((services) =>
        {
            services.AddKubernetesProbes().AddHealthChecks();
        });

        var hostedServices = host.Services.GetServices<IHostedService>().Where(service => service.GetType().Name == "TcpEndpointHealthCheckService");
        var configurations = host.Services.GetServices<IOptionsMonitor<TcpEndpointOptions>>();

        Assert.Equal(3, hostedServices.Count());
        Assert.Single(configurations);
        Assert.Single(configurations);
        var config = configurations.First();

        var livenessRegistration = new HealthCheckRegistration("liveness", Mock.Of<IHealthCheck>(), null, new[] { ProbeTags.Liveness });
        var startupRegistration = new HealthCheckRegistration("startup", Mock.Of<IHealthCheck>(), null, new[] { ProbeTags.Startup });
        var readinessRegistration = new HealthCheckRegistration("readiness", Mock.Of<IHealthCheck>(), null, new[] { ProbeTags.Readiness });

        var livenessConfig = config.Get(ProbeTags.Liveness);
        Assert.True(livenessConfig.FilterChecks!(livenessRegistration));
        Assert.False(livenessConfig.FilterChecks(startupRegistration));
        Assert.False(livenessConfig.FilterChecks(readinessRegistration));

        var startupConfig = config.Get(ProbeTags.Startup);
        Assert.False(startupConfig.FilterChecks!(livenessRegistration));
        Assert.True(startupConfig.FilterChecks(startupRegistration));
        Assert.False(startupConfig.FilterChecks(readinessRegistration));

        var readinessConfig = config.Get(ProbeTags.Readiness);
        Assert.False(readinessConfig.FilterChecks!(livenessRegistration));
        Assert.False(readinessConfig.FilterChecks(startupRegistration));
        Assert.True(readinessConfig.FilterChecks(readinessRegistration));
    }

    [Fact]
    public void AddKubernetesProbes_WithConfigureAction_RegistersAllProbes()
    {
        using var host = CreateWebHost((services) =>
        {
            services.AddKubernetesProbes(options =>
            {
                options.LivenessProbe.TcpPort = 1;
                options.StartupProbe.TcpPort = 2;
                options.ReadinessProbe.TcpPort = 3;
            }).AddHealthChecks();
        });

        var hostedServices = host.Services.GetServices<IHostedService>().Where(service => service.GetType().Name == "TcpEndpointHealthCheckService");
        var configurations = host.Services.GetServices<IOptionsMonitor<TcpEndpointOptions>>();

        Assert.Equal(3, hostedServices.Count());
        Assert.Single(configurations);
        var config = configurations.First();
        Assert.Equal(1, config.Get(ProbeTags.Liveness).TcpPort);
        Assert.Equal(2, config.Get(ProbeTags.Startup).TcpPort);
        Assert.Equal(3, config.Get(ProbeTags.Readiness).TcpPort);
    }

    [Fact]
    public void AddKubernetesProbes_WithConfigurationSection_RegistersAllProbes()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KubernetesProbes:LivenessProbe:TcpPort"] = "1",
                ["KubernetesProbes:StartupProbe:TcpPort"] = "2",
                ["KubernetesProbes:ReadinessProbe:TcpPort"] = "3",
            })
            .Build();

        using var host = CreateWebHost((services) =>
        {
            services.AddKubernetesProbes(configuration.GetSection("KubernetesProbes")).AddHealthChecks();
        });

        var hostedServices = host.Services.GetServices<IHostedService>().Where(service => service.GetType().Name == "TcpEndpointHealthCheckService");
        var configurations = host.Services.GetServices<IOptionsMonitor<TcpEndpointOptions>>();

        Assert.Equal(3, hostedServices.Count());
        Assert.Single(configurations);
        var config = configurations.First();
        Assert.Equal(1, config.Get(ProbeTags.Liveness).TcpPort);
        Assert.Equal(2, config.Get(ProbeTags.Startup).TcpPort);
        Assert.Equal(3, config.Get(ProbeTags.Readiness).TcpPort);
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
