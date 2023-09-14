// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Latency.Internal;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Latency.Test;

public class LatencyContextExtensionTest
{
    [Fact]
    public void ServiceCollection_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            LatencyContextExtensions.AddLatencyContext(null!));
    }

    [Fact]
    public void AddContext_BasicAddLatencyContext()
    {
        using var serviceProvider = new ServiceCollection()
            .AddLatencyContext()
            .BuildServiceProvider();

        var latencyContextProvider = serviceProvider.GetRequiredService<ILatencyContextProvider>();
        Assert.NotNull(latencyContextProvider);
        Assert.IsAssignableFrom<LatencyContextProvider>(latencyContextProvider);

        var latencyContextTokenIssuer = serviceProvider.GetRequiredService<ILatencyContextTokenIssuer>();
        Assert.NotNull(latencyContextTokenIssuer);
        Assert.IsAssignableFrom<LatencyContextTokenIssuer>(latencyContextTokenIssuer);
    }

    [Fact]
    public void ServiceCollection_GivenScopes_ReturnsDifferentInstanceForEachScope()
    {
        using var serviceProvider = new ServiceCollection()
            .AddLatencyContext()
            .BuildServiceProvider();

        var scope1 = serviceProvider.CreateScope();
        var scope2 = serviceProvider.CreateScope();

        // Get same instance within single scope.
        Assert.Equal(scope1.ServiceProvider.GetRequiredService<ILatencyContextProvider>(),
            scope1.ServiceProvider.GetRequiredService<ILatencyContextProvider>());
        Assert.Equal(scope1.ServiceProvider.GetRequiredService<ILatencyContextTokenIssuer>(),
            scope1.ServiceProvider.GetRequiredService<ILatencyContextTokenIssuer>());

        // Get same instance between different scopes.
        Assert.Equal(scope1.ServiceProvider.GetRequiredService<ILatencyContextProvider>(),
            scope2.ServiceProvider.GetRequiredService<ILatencyContextProvider>());
        Assert.Equal(scope1.ServiceProvider.GetRequiredService<ILatencyContextTokenIssuer>(),
            scope1.ServiceProvider.GetRequiredService<ILatencyContextTokenIssuer>());

        scope1.Dispose();
        scope2.Dispose();
    }

    [Fact]
    public void AddContext_InvokesConfig()
    {
        var invoked = false;
        using var serviceProvider = new ServiceCollection()
            .AddLatencyContext(a => { invoked = true; })
            .BuildServiceProvider();

        var latencyContextProvider = serviceProvider.GetRequiredService<ILatencyContextProvider>();
        Assert.NotNull(latencyContextProvider);
        Assert.IsAssignableFrom<LatencyContextProvider>(latencyContextProvider);

        var latencyContextTokenIssuer = serviceProvider.GetRequiredService<ILatencyContextTokenIssuer>();
        Assert.NotNull(latencyContextTokenIssuer);
        Assert.IsAssignableFrom<LatencyContextTokenIssuer>(latencyContextTokenIssuer);

        Assert.True(invoked);
    }

    [Fact]
    public void AddContext_BindsToConfigSection()
    {
        LatencyContextOptions expectedOptions = new()
        {
            ThrowOnUnregisteredNames = false
        };

        var config = GetConfigSection(expectedOptions);

        using var provider = new ServiceCollection()
            .AddLatencyContext(config)
            .BuildServiceProvider();
        var actualOptions = provider.GetRequiredService<IOptions<LatencyContextOptions>>();

        Assert.True(actualOptions.Value.ThrowOnUnregisteredNames == expectedOptions.ThrowOnUnregisteredNames);
    }

    [Fact]
    public void Options_BasicTest()
    {
        var l = new LatencyContextOptions
        {
            ThrowOnUnregisteredNames = true
        };
        Assert.True(l.ThrowOnUnregisteredNames);

        l.ThrowOnUnregisteredNames = false;
        Assert.False(l.ThrowOnUnregisteredNames);

        // Check for default values
        var o = new LatencyContextOptions();
        Assert.False(o.ThrowOnUnregisteredNames);
    }

    private static IConfigurationSection GetConfigSection(LatencyContextOptions options)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                    { $"{nameof(LatencyContextOptions)}:{nameof(options.ThrowOnUnregisteredNames)}", options.ThrowOnUnregisteredNames.ToString(CultureInfo.InvariantCulture) }
            })
            .Build()
            .GetSection($"{nameof(LatencyContextOptions)}");
    }
}
