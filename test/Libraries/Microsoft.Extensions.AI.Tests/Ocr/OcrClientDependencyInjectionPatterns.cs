// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OcrClientDependencyInjectionPatterns
{
    private IServiceCollection ServiceCollection { get; } = new ServiceCollection();

    [Fact]
    public void CanRegisterSingletonUsingFactory()
    {
        ServiceCollection.AddOcrClient(services => new TestOcrClient { Services = services })
            .Use((inner, services) => new SingletonMiddleware(inner, services));

        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<IOcrClient>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<IOcrClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IOcrClient>();

        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestOcrClient>(instance.InnerClientPublic);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingSharedInstance()
    {
        using var singleton = new TestOcrClient();
        ServiceCollection.AddKeyedOcrClient("mykey", singleton)
            .Use((inner, services) => new SingletonMiddleware(inner, services));

        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<IOcrClient>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<IOcrClient>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<IOcrClient>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<IOcrClient>("mykey");

        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestOcrClient>(instance.InnerClientPublic);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddOcrClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        _ = lifetime.HasValue
            ? sc.AddOcrClient(services => new TestOcrClient(), lifetime.Value)
            : sc.AddOcrClient(services => new TestOcrClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IOcrClient), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestOcrClient>(sd.ImplementationFactory!(null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddKeyedOcrClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        _ = lifetime.HasValue
            ? sc.AddKeyedOcrClient("key", services => new TestOcrClient(), lifetime.Value)
            : sc.AddKeyedOcrClient("key", services => new TestOcrClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IOcrClient), sd.ServiceType);
        Assert.True(sd.IsKeyedService);
        Assert.Equal("key", sd.ServiceKey);
        Assert.Null(sd.KeyedImplementationInstance);
        Assert.NotNull(sd.KeyedImplementationFactory);
        Assert.IsType<TestOcrClient>(sd.KeyedImplementationFactory!(null!, null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    public class SingletonMiddleware(IOcrClient inner, IServiceProvider services) : DelegatingOcrClient(inner)
    {
        public IOcrClient InnerClientPublic => base.InnerClient;
        public IServiceProvider Services => services;
    }
}
