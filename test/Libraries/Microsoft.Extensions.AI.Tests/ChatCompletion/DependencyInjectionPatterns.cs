// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DependencyInjectionPatterns
{
    private IServiceCollection ServiceCollection { get; } = new ServiceCollection();

    [Fact]
    public void CanRegisterSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddChatClient(services => new TestChatClient { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<IChatClient>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<IChatClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IChatClient>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestChatClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestChatClient();
        ServiceCollection.AddChatClient(singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<IChatClient>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<IChatClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IChatClient>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestChatClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddKeyedChatClient("mykey", services => new TestChatClient { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<IChatClient>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<IChatClient>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<IChatClient>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<IChatClient>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestChatClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestChatClient();
        ServiceCollection.AddKeyedChatClient("mykey", singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<IChatClient>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<IChatClient>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<IChatClient>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<IChatClient>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestChatClient>(instance.InnerClient);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddChatClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        ChatClientBuilder builder = lifetime.HasValue
            ? sc.AddChatClient(services => new TestChatClient(), lifetime.Value)
            : sc.AddChatClient(services => new TestChatClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IChatClient), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestChatClient>(sd.ImplementationFactory(null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddKeyedChatClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        ChatClientBuilder builder = lifetime.HasValue
            ? sc.AddKeyedChatClient("key", services => new TestChatClient(), lifetime.Value)
            : sc.AddKeyedChatClient("key", services => new TestChatClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IChatClient), sd.ServiceType);
        Assert.True(sd.IsKeyedService);
        Assert.Equal("key", sd.ServiceKey);
        Assert.Null(sd.KeyedImplementationInstance);
        Assert.NotNull(sd.KeyedImplementationFactory);
        Assert.IsType<TestChatClient>(sd.KeyedImplementationFactory(null!, null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddEmbeddingGenerator_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        var builder = lifetime.HasValue
            ? sc.AddEmbeddingGenerator(services => new TestEmbeddingGenerator(), lifetime.Value)
            : sc.AddEmbeddingGenerator(services => new TestEmbeddingGenerator());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IEmbeddingGenerator<string, Embedding<float>>), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestEmbeddingGenerator>(sd.ImplementationFactory(null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddKeyedEmbeddingGenerator_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        var builder = lifetime.HasValue
            ? sc.AddKeyedEmbeddingGenerator("key", services => new TestEmbeddingGenerator(), lifetime.Value)
            : sc.AddKeyedEmbeddingGenerator("key", services => new TestEmbeddingGenerator());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IEmbeddingGenerator<string, Embedding<float>>), sd.ServiceType);
        Assert.True(sd.IsKeyedService);
        Assert.Equal("key", sd.ServiceKey);
        Assert.Null(sd.KeyedImplementationInstance);
        Assert.NotNull(sd.KeyedImplementationFactory);
        Assert.IsType<TestEmbeddingGenerator>(sd.KeyedImplementationFactory(null!, null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    public class SingletonMiddleware(IChatClient inner, IServiceProvider services) : DelegatingChatClient(inner)
    {
        public new IChatClient InnerClient => base.InnerClient;
        public IServiceProvider Services => services;
    }
}
