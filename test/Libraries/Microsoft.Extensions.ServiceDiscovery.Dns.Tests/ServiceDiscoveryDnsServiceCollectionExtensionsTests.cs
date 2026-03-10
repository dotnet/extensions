// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Tests;

public class ServiceDiscoveryDnsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDnsServiceEndpointProviderShouldRegisterDependentServices()
    {
        var services = new ServiceCollection();
        services.AddDnsServiceEndpointProvider();

        using var serviceProvider = services.BuildServiceProvider(true);

        var exception = Record.Exception(() => serviceProvider.GetServices<IServiceEndpointProviderFactory>());
        Assert.Null(exception);
    }

    [Fact]
    public void AddDnsSrvServiceEndpointProviderShouldRegisterDependentServices()
    {
        var services = new ServiceCollection();
        services.AddDnsSrvServiceEndpointProvider();

        using var serviceProvider = services.BuildServiceProvider(true);

        var exception = Record.Exception(() => serviceProvider.GetServices<IServiceEndpointProviderFactory>());
        Assert.Null(exception);
    }

    [Fact]
    public void ConfigureDnsResolverShouldThrowWhenServersIsNull()
    {
        var services = new ServiceCollection();
        services.ConfigureDnsResolver(options => options.Servers = null!);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<DnsResolverOptions>>();

        var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
        Assert.Equal("Servers must not be null.", exception.Message);
    }

    [Fact]
    public void ConfigureDnsResolverShouldThrowWhenMaxAttemptsIsZero()
    {
        var services = new ServiceCollection();
        services.ConfigureDnsResolver(options => options.MaxAttempts = 0);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<DnsResolverOptions>>();

        var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
        Assert.Equal("MaxAttempts must be one or greater.", exception.Message);
    }

    [Fact]
    public void ConfigureDnsResolverShouldThrowWhenTimeoutIsZero()
    {
        var services = new ServiceCollection();
        services.ConfigureDnsResolver(options => options.Timeout = TimeSpan.Zero);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<DnsResolverOptions>>();

        var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
        Assert.Equal("Timeout must not be negative or zero.", exception.Message);
    }

    [Fact]
    public void ConfigureDnsResolverShouldThrowWhenTimeoutExceedsMaximum()
    {
        var services = new ServiceCollection();
        services.ConfigureDnsResolver(options => options.Timeout = TimeSpan.FromMilliseconds(1L + int.MaxValue));

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<DnsResolverOptions>>();

        var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
        Assert.Equal("Timeout must not be greater than 2147483647 milliseconds.", exception.Message);
    }
}
