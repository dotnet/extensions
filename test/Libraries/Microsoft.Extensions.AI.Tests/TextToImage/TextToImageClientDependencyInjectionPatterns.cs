// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ImageClientDependencyInjectionPatterns
{
    private IServiceCollection ServiceCollection { get; } = new ServiceCollection();

    [Fact]
    public void CanRegisterSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddImageClient(services => new TestImageClient { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<IImageClient>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<IImageClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IImageClient>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestImageClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestImageClient();
        ServiceCollection.AddImageClient(singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<IImageClient>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<IImageClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IImageClient>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestImageClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddKeyedImageClient("mykey", services => new TestImageClient { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<IImageClient>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<IImageClient>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<IImageClient>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<IImageClient>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestImageClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestImageClient();
        ServiceCollection.AddKeyedImageClient("mykey", singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<IImageClient>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<IImageClient>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<IImageClient>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<IImageClient>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestImageClient>(instance.InnerClient);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddImageClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        ImageClientBuilder builder = lifetime.HasValue
            ? sc.AddImageClient(services => new TestImageClient(), lifetime.Value)
            : sc.AddImageClient(services => new TestImageClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IImageClient), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestImageClient>(sd.ImplementationFactory(null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddKeyedImageClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        ImageClientBuilder builder = lifetime.HasValue
            ? sc.AddKeyedImageClient("key", services => new TestImageClient(), lifetime.Value)
            : sc.AddKeyedImageClient("key", services => new TestImageClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IImageClient), sd.ServiceType);
        Assert.True(sd.IsKeyedService);
        Assert.Equal("key", sd.ServiceKey);
        Assert.Null(sd.KeyedImplementationInstance);
        Assert.NotNull(sd.KeyedImplementationFactory);
        Assert.IsType<TestImageClient>(sd.KeyedImplementationFactory(null!, null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    public class SingletonMiddleware(IImageClient inner, IServiceProvider services) : DelegatingImageClient(inner)
    {
        public new IImageClient InnerClient => base.InnerClient;
        public IServiceProvider Services => services;
    }
}
