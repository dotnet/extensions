// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ImageGeneratorDependencyInjectionPatterns
{
    private IServiceCollection ServiceCollection { get; } = new ServiceCollection();

    [Fact]
    public void CanRegisterSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddImageGenerator(services => new TestImageGenerator { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<IImageGenerator>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<IImageGenerator>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IImageGenerator>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestImageGenerator>(instance.InnerGenerator);
    }

    [Fact]
    public void CanRegisterSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestImageGenerator();
        ServiceCollection.AddImageGenerator(singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<IImageGenerator>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<IImageGenerator>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IImageGenerator>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestImageGenerator>(instance.InnerGenerator);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddKeyedImageGenerator("mykey", services => new TestImageGenerator { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<IImageGenerator>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<IImageGenerator>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<IImageGenerator>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<IImageGenerator>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestImageGenerator>(instance.InnerGenerator);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestImageGenerator();
        ServiceCollection.AddKeyedImageGenerator("mykey", singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<IImageGenerator>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<IImageGenerator>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<IImageGenerator>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<IImageGenerator>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestImageGenerator>(instance.InnerGenerator);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddImageGenerator_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        ImageGeneratorBuilder builder = lifetime.HasValue
            ? sc.AddImageGenerator(services => new TestImageGenerator(), lifetime.Value)
            : sc.AddImageGenerator(services => new TestImageGenerator());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IImageGenerator), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestImageGenerator>(sd.ImplementationFactory(null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddKeyedImageGenerator_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        ImageGeneratorBuilder builder = lifetime.HasValue
            ? sc.AddKeyedImageGenerator("key", services => new TestImageGenerator(), lifetime.Value)
            : sc.AddKeyedImageGenerator("key", services => new TestImageGenerator());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IImageGenerator), sd.ServiceType);
        Assert.True(sd.IsKeyedService);
        Assert.Equal("key", sd.ServiceKey);
        Assert.Null(sd.KeyedImplementationInstance);
        Assert.NotNull(sd.KeyedImplementationFactory);
        Assert.IsType<TestImageGenerator>(sd.KeyedImplementationFactory(null!, null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Fact]
    public void AddKeyedImageGenerator_WorksWithNullServiceKey()
    {
        ServiceCollection sc = new();
        sc.AddKeyedImageGenerator(null, _ => new TestImageGenerator());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IImageGenerator), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ServiceKey);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestImageGenerator>(sd.ImplementationFactory(null!));
        Assert.Equal(ServiceLifetime.Singleton, sd.Lifetime);
    }

    public class SingletonMiddleware(IImageGenerator inner, IServiceProvider services) : DelegatingImageGenerator(inner)
    {
        public new IImageGenerator InnerGenerator => base.InnerGenerator;
        public IServiceProvider Services => services;
    }
}
