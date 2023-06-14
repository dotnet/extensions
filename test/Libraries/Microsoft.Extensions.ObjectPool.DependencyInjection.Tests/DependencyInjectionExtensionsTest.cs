// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool.Test.TestResources;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.ObjectPool.Test;

public class DependencyInjectionExtensionsTest
{
    [Fact]
    public void AddPooled_Throws_WhenNullService()
    {
        Assert.Throws<ArgumentNullException>(() => ObjectPoolServiceCollectionExtensions.AddPooled<TestClass>(null!));
        Assert.Throws<ArgumentNullException>(() => ObjectPoolServiceCollectionExtensions.AddPooled<ITestClass, TestClass>(null!));
    }

    [Fact]
    public void ConfigurePools_ThrowsOnUnparsableMaximumCapacity()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>($"My:Pools:{typeof(TestClass).FullName!}", "twenty!"),
            new KeyValuePair<string, string?>($"My:Pools:{typeof(TestDependency).FullName!}", "4096"),
        });

        var exception = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().ConfigurePools(builder.Build().GetSection("My:Pools")));

        Assert.StartsWith(
            "Can't parse 'Microsoft.Extensions.ObjectPool.Test.TestResources.TestClass' value 'twenty!' to integer.",
            exception.Message);
    }

    [Fact]
    public void ConfigurePools_ConfiguresDependencyInjectionPoolOptions()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>($"My:Pools:{typeof(TestClass).FullName!}", "2048"),
            new KeyValuePair<string, string?>($"My:Pools:{typeof(TestDependency).FullName!}", "4096"),
        });

        var services = new ServiceCollection().ConfigurePools(builder.Build().GetSection("My:Pools"));
        using var provider = services.BuildServiceProvider();

        var sut = provider.GetRequiredService<IOptionsMonitor<DependencyInjectionPoolOptions>>();

        Assert.Equal(2048, sut.Get(typeof(TestClass).FullName!).Capacity);
        Assert.Equal(4096, sut.Get(typeof(TestDependency).FullName!).Capacity);
    }

    [Fact]
    public void ConfiguresDependencyInjectionPoolOptions()
    {
        var services = new ServiceCollection()
            .Configure<DependencyInjectionPoolOptions>(typeof(TestClass).FullName, options => options.Capacity = 2048)
            .Configure<DependencyInjectionPoolOptions>(typeof(TestDependency).FullName, options => options.Capacity = 4096);

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var options = provider.GetRequiredService<IOptionsMonitor<DependencyInjectionPoolOptions>>();

        Assert.Equal(DependencyInjectionPoolOptions.DefaultCapacity, options.CurrentValue.Capacity);
        Assert.Equal(DependencyInjectionPoolOptions.DefaultCapacity, options.Get(null).Capacity);
        Assert.Equal(DependencyInjectionPoolOptions.DefaultCapacity, options.Get(typeof(object).FullName!).Capacity);
        Assert.Equal(2048, options.Get(typeof(TestClass).FullName!).Capacity);
        Assert.Equal(4096, options.Get(typeof(TestDependency).FullName!).Capacity);
    }

    [Fact]
    public void ConfigurePools_ThrowsOnNullSection()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().ConfigurePools(null!));
    }

    [Fact]
    public void AddPool_ServiceTypeOnly_AddsPool()
    {
        var services = new ServiceCollection().AddPooled<TestDependency>();

        var pool = services.BuildServiceProvider(validateScopes: true).GetService<ObjectPool<TestDependency>>();
        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.NotNull(pool);
    }

    [Fact]
    public void AddPool_NewTImplementation_AddsPool()
    {
        var services = new ServiceCollection()
                    .AddSingleton<TestDependency>()
                    .AddPooled<ITestClass, TestClass>()
                    .AddPooled<ITestClass, OtherTestClass>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var pool = provider.GetRequiredService<ObjectPool<ITestClass>>();

        var pooled = pool.Get();

        Assert.NotNull(pooled);
        Assert.True(pooled is OtherTestClass);
    }

    [Fact]
    public void AddPool_ServiceTypeOnlyWithCapacity_AddsPoolAndSetsCapacity()
    {
        var services = new ServiceCollection().AddPooled<TestDependency>(options => options.Capacity = 64);

        var pool = services.BuildServiceProvider().GetService<ObjectPool<TestDependency>>();
        using var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DependencyInjectionPoolOptions>>();

        Assert.NotNull(pool);
        Assert.Equal(64, optionsMonitor.Get(typeof(TestDependency).FullName).Capacity);
    }

    [Fact]
    public void AddPool_ServiceAndImplementationType_AddsPool()
    {
        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPooled<ITestClass, TestClass>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var pool = provider.GetService<ObjectPool<ITestClass>>();

        Assert.NotNull(pool);
        Assert.Equal(TestDependency.DefaultMessage, pool!.Get().ReadMessage());
    }

    [Fact]
    public void AddPool_ServiceAndImplementationTypeWithCapacity_AddsPoolAndSetsCapacity()
    {
        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPooled<ITestClass, TestClass>(options => options.Capacity = 64);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var pool = provider.GetService<ObjectPool<ITestClass>>();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<DependencyInjectionPoolOptions>>();

        Assert.NotNull(pool);
        Assert.Equal(TestDependency.DefaultMessage, pool!.Get().ReadMessage());
        Assert.Equal(64, optionsMonitor.Get(typeof(ITestClass).FullName).Capacity);
    }

    [Fact]
    public void AddPool_ReturnedPooled_CallsTryReset()
    {
        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPooled<ITestClass, TestClass>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var pool = provider.GetRequiredService<ObjectPool<ITestClass>>();

        var pooled = pool.Get();
        pool.Return(pooled);

        Assert.Equal(1, pooled.ResetCalled);
        Assert.Equal(0, pooled.DisposedCalled);
    }

    [Fact]
    public void AddPool_CapacityReached_CreatesNew()
    {
        var capacity = 64;

        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPooled<ITestClass, TestClass>(options => options.Capacity = capacity);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var pool = provider.GetRequiredService<ObjectPool<ITestClass>>();

        var instances = new ITestClass[capacity];

        for (var i = 0; i < capacity; i++)
        {
            instances[i] = pool.Get();
        }

        for (var i = 0; i < capacity; i++)
        {
            pool.Return(instances[i]);
        }

        for (var i = 0; i < capacity; i++)
        {
            var pooled = pool.Get();
            Assert.Contains(pooled, instances);
        }

        var newPooled = pool.Get();
        Assert.DoesNotContain(newPooled, instances);
    }

    [Fact]
    public void AddPool_DependsOnScoped_Invalid()
    {
        // Pooled classes are resolved using the root service provider,
        // hence they should not be able to have scoped dependencies

        var services = new ServiceCollection()
            .AddScoped<TestDependency>()
            .AddPooled<ITestClass, TestClass>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ObjectPool<ITestClass>>().Get());
    }

    [Fact]
    public void PooledHelperReturnsScopedInstances_SameScope()
    {
        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPooled<TestClass>()
            .AddScoped<Pooled<TestClass>>()
            .AddScoped<ITestClass>(provider => provider.GetRequiredService<Pooled<TestClass>>().Object)
            ;

        using var provider = services.BuildServiceProvider(validateScopes: true);

        ITestClass resolved1;
        ITestClass resolved2;

        // Because these are scoped, resolved1 and resolved2 are the same instance
        // and a single reference is disposed.

        using (var scope = provider.CreateScope())
        {
            resolved1 = scope.ServiceProvider.GetRequiredService<ITestClass>();
            resolved2 = scope.ServiceProvider.GetRequiredService<ITestClass>();
        }

        Assert.NotNull(resolved1);
        Assert.NotNull(resolved2);
        Assert.Same(resolved1, resolved2);

        Assert.Equal(1, resolved1.DisposedCalled); // Pooled instances are disposed by the scope when it is disposed
        Assert.Equal(1, resolved1.ResetCalled);
    }

    [Fact]
    public void PooledHelperReturnsTransientInstances_DifferentScopes()
    {
        // Resolving the same class from two different scopes should return two
        // distinct instances.

        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPooled<TestClass>()
            .AddTransient<Pooled<TestClass>>()
            .AddTransient<ITestClass>(provider => provider.GetRequiredService<Pooled<TestClass>>().Object)
            ;

        using var provider = services.BuildServiceProvider(validateScopes: true);

        ITestClass resolved1;
        ITestClass resolved2;
        ITestClass resolved3;
        ITestClass resolved4;

        var scope1 = provider.CreateScope();
        resolved1 = scope1.ServiceProvider.GetRequiredService<ITestClass>();
        resolved2 = scope1.ServiceProvider.GetRequiredService<ITestClass>();

        var scope2 = provider.CreateScope();
        resolved3 = scope2.ServiceProvider.GetRequiredService<ITestClass>();
        resolved4 = scope2.ServiceProvider.GetRequiredService<ITestClass>();

        scope1.Dispose();
        scope2.Dispose();

        Assert.NotNull(resolved1);
        Assert.NotNull(resolved2);
        Assert.NotNull(resolved3);
        Assert.NotNull(resolved4);

        // Not the same instances as they are transient from a DI perspective so
        // should each call .Object on a new Pooled<TestClass> instance

        Assert.NotSame(resolved1, resolved2);
        Assert.NotSame(resolved3, resolved4);

        // All pooled objects should be returned and disposed once

        Assert.Equal(1, resolved1.DisposedCalled);
        Assert.Equal(1, resolved1.ResetCalled);

        Assert.Equal(1, resolved2.DisposedCalled);
        Assert.Equal(1, resolved2.ResetCalled);

        Assert.Equal(1, resolved3.DisposedCalled);
        Assert.Equal(1, resolved3.ResetCalled);

        Assert.Equal(1, resolved4.DisposedCalled);
        Assert.Equal(1, resolved4.ResetCalled);
    }

    [Fact]
    public void PooledHelperReturnsScopedInstances_DifferentScopes()
    {
        // Resolving the same class from two different scopes should return two
        // distinct instances.

        var services = new ServiceCollection()
            .AddSingleton<TestDependency>()
            .AddPooled<TestClass>()
            .AddScoped<Pooled<TestClass>>()
            .AddScoped<ITestClass>(provider => provider.GetRequiredService<Pooled<TestClass>>().Object)
            ;

        using var provider = services.BuildServiceProvider(validateScopes: true);

        ITestClass resolved1;
        ITestClass resolved2;
        ITestClass resolved3;
        ITestClass resolved4;

        var scope1 = provider.CreateScope();
        resolved1 = scope1.ServiceProvider.GetRequiredService<ITestClass>();
        resolved2 = scope1.ServiceProvider.GetRequiredService<ITestClass>();

        var scope2 = provider.CreateScope();
        resolved3 = scope2.ServiceProvider.GetRequiredService<ITestClass>();
        resolved4 = scope2.ServiceProvider.GetRequiredService<ITestClass>();

        scope1.Dispose();
        scope2.Dispose();

        Assert.NotNull(resolved1);
        Assert.NotNull(resolved2);
        Assert.NotNull(resolved3);
        Assert.NotNull(resolved4);

        // Same instances when resolved from the same scope
        // Different scopes get different pooled instances

        Assert.Same(resolved1, resolved2);
        Assert.Same(resolved3, resolved4);
        Assert.NotSame(resolved1, resolved3);

        // All pooled objects should be returned and disposed once

        Assert.Equal(1, resolved1.DisposedCalled);
        Assert.Equal(1, resolved1.ResetCalled);

        Assert.Equal(1, resolved3.DisposedCalled);
        Assert.Equal(1, resolved3.ResetCalled);
    }
}
