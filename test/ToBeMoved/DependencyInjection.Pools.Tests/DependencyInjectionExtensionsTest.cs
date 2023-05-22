// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Pools.Test.TestResources;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Pools.Test;

public class DependencyInjectionExtensionsTest
{
    [Fact]
    public void ConfigurePools_ConfiguresPoolOptions()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>($"R9:Pools:{typeof(TestClass).FullName!}", "2048"),
            new KeyValuePair<string, string?>($"R9:Pools:{typeof(TestDependency).FullName!}", "4096"),
        });

        var services = new ServiceCollection().ConfigurePools(builder.Build().GetSection("R9:Pools"));
        using var provider = services.BuildServiceProvider();

        var sut = provider.GetRequiredService<IOptionsMonitor<PoolOptions>>();

        Assert.Equal(2048, sut.Get(typeof(TestClass).FullName!).Capacity);
        Assert.Equal(4096, sut.Get(typeof(TestDependency).FullName!).Capacity);
    }

    [Fact]
    public void ConfigurePools_ThrowsOnUnparsableMaximumCapacity()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>($"R9:Pools:{typeof(TestClass).FullName!}", "twenty!"),
            new KeyValuePair<string, string?>($"R9:Pools:{typeof(TestDependency).FullName!}", "4096"),
        });

        var exception = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().ConfigurePools(builder.Build().GetSection("R9:Pools")));

        Assert.StartsWith(
            "Can't parse 'Microsoft.Extensions.DependencyInjection.Pools.Test.TestResources.TestClass' value 'twenty!' to integer.",
            exception.Message);
    }

    [Fact]
    public void ConfigurePools_ThrowsOnNullSection()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new ServiceCollection().ConfigurePools(null!));
        Assert.StartsWith("Value cannot be null.", exception.Message);
    }

    [Fact]
    public void AddPool_ServiceTypeOnly_AddsPool()
    {
        var services = new ServiceCollection().AddPool<TestDependency>();

        var sut = services.BuildServiceProvider().GetService<ObjectPool<TestDependency>>();
        using var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PoolOptions>>();

        Assert.NotNull(sut);
        Assert.Equal(1024, optionsMonitor.Get(typeof(TestDependency).FullName).Capacity);
    }

    [Fact]
    public void AddPool_ServiceTypeOnlyWithCapacity_AddsPoolAndSetsCapacity()
    {
        var services = new ServiceCollection().AddPool<TestDependency>(options => options.Capacity = 64);

        var sut = services.BuildServiceProvider().GetService<ObjectPool<TestDependency>>();
        using var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PoolOptions>>();

        Assert.NotNull(sut);
        Assert.Equal(64, optionsMonitor.Get(typeof(TestDependency).FullName).Capacity);
    }

    [Fact]
    public void AddPool_ServiceAndImplementationType_AddsPool()
    {
        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPool<ITestClass, TestClass>();

        using var provider = services.BuildServiceProvider();
        var sut = provider.GetService<ObjectPool<ITestClass>>();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PoolOptions>>();

        Assert.NotNull(sut);
        Assert.Equal(TestDependency.Message, sut!.Get().ReadMessage());
        Assert.Equal(1024, optionsMonitor.Get(typeof(ITestClass).FullName).Capacity);
    }

    [Fact]
    public void AddPool_ServiceAndImplementationTypeWithCapacity_AddsPoolAndSetsCapacity()
    {
        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPool<ITestClass, TestClass>(options => options.Capacity = 64);

        using var provider = services.BuildServiceProvider();
        var sut = provider.GetService<ObjectPool<ITestClass>>();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PoolOptions>>();

        Assert.NotNull(sut);
        Assert.Equal(TestDependency.Message, sut!.Get().ReadMessage());
        Assert.Equal(64, optionsMonitor.Get(typeof(ITestClass).FullName).Capacity);
    }
}
