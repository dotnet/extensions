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
    public void CanRegisterSingletonUsingGenericType()
    {
        // Arrange/Act
        ServiceCollection.AddSingleton(services => new TestChatClient { Services = services });
        ServiceCollection.AddChatClient<TestChatClient>()
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

    public class SingletonMiddleware(IServiceProvider services, IChatClient inner) : DelegatingChatClient(inner)
    {
        public new IChatClient InnerClient => base.InnerClient;
        public IServiceProvider Services => services;
    }
}
