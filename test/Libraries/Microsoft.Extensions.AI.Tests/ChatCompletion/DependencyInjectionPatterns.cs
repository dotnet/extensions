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
    public void CanRegisterScopedUsingGenericType()
    {
        // Arrange/Act
        ServiceCollection.AddChatClient(builder => builder
            .UseScopedMiddleware()
            .Use(new TestChatClient()));

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<IChatClient>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<IChatClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IChatClient>();

        // Each scope gets a distinct outer *AND* inner client
        var outer1 = Assert.IsType<ScopedChatClient>(instance1);
        var outer2 = Assert.IsType<ScopedChatClient>(instance2);
        var inner1 = Assert.IsType<TestChatClient>(((ScopedChatClient)instance1).InnerClient);
        var inner2 = Assert.IsType<TestChatClient>(((ScopedChatClient)instance2).InnerClient);

        Assert.NotSame(outer1.Services, outer2.Services);
        Assert.NotSame(instance1, instance2);
        Assert.NotSame(inner1, inner2);
        Assert.Same(instance1, instance1Copy); // From the same scope
    }

    [Fact]
    public void CanRegisterScopedUsingFactory()
    {
        // Arrange/Act
        ServiceCollection.AddChatClient(builder =>
        {
            builder.UseScopedMiddleware();
            return builder.Use(new TestChatClient { Services = builder.Services });
        });

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<IChatClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IChatClient>();

        // Each scope gets a distinct outer *AND* inner client
        var outer1 = Assert.IsType<ScopedChatClient>(instance1);
        var outer2 = Assert.IsType<ScopedChatClient>(instance2);
        var inner1 = Assert.IsType<TestChatClient>(((ScopedChatClient)instance1).InnerClient);
        var inner2 = Assert.IsType<TestChatClient>(((ScopedChatClient)instance2).InnerClient);

        Assert.Same(outer1.Services, inner1.Services);
        Assert.Same(outer2.Services, inner2.Services);
        Assert.NotSame(outer1.Services, outer2.Services);
    }

    [Fact]
    public void CanRegisterScopedUsingSharedInstance()
    {
        // Arrange/Act
        using var singleton = new TestChatClient();
        ServiceCollection.AddChatClient(builder =>
        {
            builder.UseScopedMiddleware();
            return builder.Use(singleton);
        });

        // Assert
        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();
        var instance1 = scope1.ServiceProvider.GetRequiredService<IChatClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IChatClient>();

        // Each scope gets a distinct outer instance, but the same inner client
        Assert.IsType<ScopedChatClient>(instance1);
        Assert.IsType<ScopedChatClient>(instance2);
        Assert.Same(singleton, ((ScopedChatClient)instance1).InnerClient);
        Assert.Same(singleton, ((ScopedChatClient)instance2).InnerClient);
    }

    public class ScopedChatClient(IServiceProvider services, IChatClient inner) : DelegatingChatClient(inner)
    {
        public new IChatClient InnerClient => base.InnerClient;
        public IServiceProvider Services => services;
    }
}
