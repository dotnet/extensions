// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class SpeechToTextClientDependencyInjectionPatterns
{
    private IServiceCollection ServiceCollection { get; } = new ServiceCollection();

    [Fact]
    public void CanRegisterSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddSpeechToTextClient(services => new TestSpeechToTextClient { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<ISpeechToTextClient>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<ISpeechToTextClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<ISpeechToTextClient>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestSpeechToTextClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestSpeechToTextClient();
        ServiceCollection.AddSpeechToTextClient(singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<ISpeechToTextClient>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<ISpeechToTextClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<ISpeechToTextClient>();

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestSpeechToTextClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddKeyedSpeechToTextClient("mykey", services => new TestSpeechToTextClient { Services = services })
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<ISpeechToTextClient>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<ISpeechToTextClient>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<ISpeechToTextClient>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<ISpeechToTextClient>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestSpeechToTextClient>(instance.InnerClient);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestSpeechToTextClient();
        ServiceCollection.AddKeyedSpeechToTextClient("mykey", singleton)
            .UseSingletonMiddleware();

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<ISpeechToTextClient>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<ISpeechToTextClient>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<ISpeechToTextClient>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<ISpeechToTextClient>("mykey");

        // Each scope gets the same instance, because it's singleton
        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestSpeechToTextClient>(instance.InnerClient);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddSpeechToTextClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        SpeechToTextClientBuilder builder = lifetime.HasValue
            ? sc.AddSpeechToTextClient(services => new TestSpeechToTextClient(), lifetime.Value)
            : sc.AddSpeechToTextClient(services => new TestSpeechToTextClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(ISpeechToTextClient), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestSpeechToTextClient>(sd.ImplementationFactory(null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddKeyedSpeechToTextClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        SpeechToTextClientBuilder builder = lifetime.HasValue
            ? sc.AddKeyedSpeechToTextClient("key", services => new TestSpeechToTextClient(), lifetime.Value)
            : sc.AddKeyedSpeechToTextClient("key", services => new TestSpeechToTextClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(ISpeechToTextClient), sd.ServiceType);
        Assert.True(sd.IsKeyedService);
        Assert.Equal("key", sd.ServiceKey);
        Assert.Null(sd.KeyedImplementationInstance);
        Assert.NotNull(sd.KeyedImplementationFactory);
        Assert.IsType<TestSpeechToTextClient>(sd.KeyedImplementationFactory(null!, null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    public class SingletonMiddleware(ISpeechToTextClient inner, IServiceProvider services) : DelegatingSpeechToTextClient(inner)
    {
        public new ISpeechToTextClient InnerClient => base.InnerClient;
        public IServiceProvider Services => services;
    }
}
