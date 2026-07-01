// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class VideoGeneratorDependencyInjectionPatterns
{
    private IServiceCollection ServiceCollection { get; } = new ServiceCollection();

    [Fact]
    public void CanRegisterSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddVideoGenerator(services => new TestVideoGenerator { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<IVideoGenerator>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<IVideoGenerator>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IVideoGenerator>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestVideoGenerator>(instance.InnerGenerator);
    }

    [Fact]
    public void CanRegisterSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestVideoGenerator();
        ServiceCollection.AddVideoGenerator(singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<IVideoGenerator>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<IVideoGenerator>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IVideoGenerator>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestVideoGenerator>(instance.InnerGenerator);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddKeyedVideoGenerator("mykey", services => new TestVideoGenerator { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<IVideoGenerator>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<IVideoGenerator>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<IVideoGenerator>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<IVideoGenerator>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestVideoGenerator>(instance.InnerGenerator);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestVideoGenerator();
        ServiceCollection.AddKeyedVideoGenerator("mykey", singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<IVideoGenerator>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<IVideoGenerator>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<IVideoGenerator>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<IVideoGenerator>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestVideoGenerator>(instance.InnerGenerator);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddVideoGenerator_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        VideoGeneratorBuilder builder = lifetime.HasValue
            ? sc.AddVideoGenerator(services => new TestVideoGenerator(), lifetime.Value)
            : sc.AddVideoGenerator(services => new TestVideoGenerator());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IVideoGenerator), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestVideoGenerator>(sd.ImplementationFactory(null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddKeyedVideoGenerator_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        VideoGeneratorBuilder builder = lifetime.HasValue
            ? sc.AddKeyedVideoGenerator("key", services => new TestVideoGenerator(), lifetime.Value)
            : sc.AddKeyedVideoGenerator("key", services => new TestVideoGenerator());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IVideoGenerator), sd.ServiceType);
        Assert.True(sd.IsKeyedService);
        Assert.Equal("key", sd.ServiceKey);
        Assert.Null(sd.KeyedImplementationInstance);
        Assert.NotNull(sd.KeyedImplementationFactory);
        Assert.IsType<TestVideoGenerator>(sd.KeyedImplementationFactory(null!, null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Fact]
    public void AddKeyedVideoGenerator_WorksWithNullServiceKey()
    {
        ServiceCollection sc = new();
        sc.AddKeyedVideoGenerator(null, _ => new TestVideoGenerator());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IVideoGenerator), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ServiceKey);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestVideoGenerator>(sd.ImplementationFactory(null!));
        Assert.Equal(ServiceLifetime.Singleton, sd.Lifetime);
    }

    public class SingletonMiddleware(IVideoGenerator inner, IServiceProvider services) : DelegatingVideoGenerator(inner)
    {
        public new IVideoGenerator InnerGenerator => base.InnerGenerator;
        public IServiceProvider Services => services;
    }
}
