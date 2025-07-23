// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class TextToImageClientDependencyInjectionPatterns
{
    private IServiceCollection ServiceCollection { get; } = new ServiceCollection();

    [Fact]
    public void CanRegisterSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddTextToImageClient(services => new TestTextToImageClient { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<ITextToImageClient>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<ITextToImageClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<ITextToImageClient>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestTextToImageClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestTextToImageClient();
        ServiceCollection.AddTextToImageClient(singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<ITextToImageClient>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<ITextToImageClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<ITextToImageClient>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestTextToImageClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddKeyedTextToImageClient("mykey", services => new TestTextToImageClient { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<ITextToImageClient>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<ITextToImageClient>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<ITextToImageClient>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<ITextToImageClient>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestTextToImageClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestTextToImageClient();
        ServiceCollection.AddKeyedTextToImageClient("mykey", singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<ITextToImageClient>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<ITextToImageClient>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<ITextToImageClient>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<ITextToImageClient>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestTextToImageClient>(instance.InnerClient);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddTextToImageClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        TextToImageClientBuilder builder = lifetime.HasValue
            ? sc.AddTextToImageClient(services => new TestTextToImageClient(), lifetime.Value)
            : sc.AddTextToImageClient(services => new TestTextToImageClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(ITextToImageClient), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestTextToImageClient>(sd.ImplementationFactory(null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddKeyedTextToImageClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        TextToImageClientBuilder builder = lifetime.HasValue
            ? sc.AddKeyedTextToImageClient("key", services => new TestTextToImageClient(), lifetime.Value)
            : sc.AddKeyedTextToImageClient("key", services => new TestTextToImageClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(ITextToImageClient), sd.ServiceType);
        Assert.True(sd.IsKeyedService);
        Assert.Equal("key", sd.ServiceKey);
        Assert.Null(sd.KeyedImplementationInstance);
        Assert.NotNull(sd.KeyedImplementationFactory);
        Assert.IsType<TestTextToImageClient>(sd.KeyedImplementationFactory(null!, null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    public class SingletonMiddleware(ITextToImageClient inner, IServiceProvider services) : DelegatingTextToImageClient(inner)
    {
        public new ITextToImageClient InnerClient => base.InnerClient;
        public IServiceProvider Services => services;
    }
}
