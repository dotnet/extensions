// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class TextToSpeechClientDependencyInjectionPatterns
{
    private IServiceCollection ServiceCollection { get; } = new ServiceCollection();

    [Fact]
    public void CanRegisterSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddTextToSpeechClient(services => new TestTextToSpeechClient { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<ITextToSpeechClient>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<ITextToSpeechClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<ITextToSpeechClient>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestTextToSpeechClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestTextToSpeechClient();
        ServiceCollection.AddTextToSpeechClient(singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<ITextToSpeechClient>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<ITextToSpeechClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<ITextToSpeechClient>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestTextToSpeechClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddKeyedTextToSpeechClient("mykey", services => new TestTextToSpeechClient { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<ITextToSpeechClient>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<ITextToSpeechClient>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<ITextToSpeechClient>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<ITextToSpeechClient>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestTextToSpeechClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestTextToSpeechClient();
        ServiceCollection.AddKeyedTextToSpeechClient("mykey", singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<ITextToSpeechClient>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<ITextToSpeechClient>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<ITextToSpeechClient>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<ITextToSpeechClient>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestTextToSpeechClient>(instance.InnerClient);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddTextToSpeechClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        TextToSpeechClientBuilder builder = lifetime.HasValue
            ? sc.AddTextToSpeechClient(services => new TestTextToSpeechClient(), lifetime.Value)
            : sc.AddTextToSpeechClient(services => new TestTextToSpeechClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(ITextToSpeechClient), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestTextToSpeechClient>(sd.ImplementationFactory(null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddKeyedTextToSpeechClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        TextToSpeechClientBuilder builder = lifetime.HasValue
            ? sc.AddKeyedTextToSpeechClient("key", services => new TestTextToSpeechClient(), lifetime.Value)
            : sc.AddKeyedTextToSpeechClient("key", services => new TestTextToSpeechClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(ITextToSpeechClient), sd.ServiceType);
        Assert.True(sd.IsKeyedService);
        Assert.Equal("key", sd.ServiceKey);
        Assert.Null(sd.KeyedImplementationInstance);
        Assert.NotNull(sd.KeyedImplementationFactory);
        Assert.IsType<TestTextToSpeechClient>(sd.KeyedImplementationFactory(null!, null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Fact]
    public void AddKeyedTextToSpeechClient_WorksWithNullServiceKey()
    {
        ServiceCollection sc = new();
        sc.AddKeyedTextToSpeechClient(null, _ => new TestTextToSpeechClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(ITextToSpeechClient), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ServiceKey);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestTextToSpeechClient>(sd.ImplementationFactory(null!));
        Assert.Equal(ServiceLifetime.Singleton, sd.Lifetime);
    }

    public class SingletonMiddleware(ITextToSpeechClient inner, IServiceProvider services) : DelegatingTextToSpeechClient(inner)
    {
        public new ITextToSpeechClient InnerClient => base.InnerClient;
        public IServiceProvider Services => services;
    }
}
