// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.DocumentExtraction;

public class DocumentExtractionClientDependencyInjectionPatterns
{
    private IServiceCollection ServiceCollection { get; } = new ServiceCollection();

    [Fact]
    public void CanRegisterSingletonUsingFactory()
    {
        ServiceCollection.AddDocumentExtractionClient(services => new TestDocumentExtractionClient { Services = services })
            .Use((inner, services) => new SingletonMiddleware(inner, services));

        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<IDocumentExtractionClient>();
        var instance1Copy = scope1.ServiceProvider.GetRequiredService<IDocumentExtractionClient>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IDocumentExtractionClient>();

        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestDocumentExtractionClient>(instance.InnerClientPublic);
    }

    [Fact]
    public void CanRegisterKeyedSingletonUsingSharedInstance()
    {
        using var singleton = new TestDocumentExtractionClient();
        ServiceCollection.AddKeyedDocumentExtractionClient("mykey", singleton)
            .Use((inner, services) => new SingletonMiddleware(inner, services));

        var services = ServiceCollection.BuildServiceProvider();
        using var scope1 = services.CreateScope();
        using var scope2 = services.CreateScope();

        Assert.Null(services.GetService<IDocumentExtractionClient>());

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<IDocumentExtractionClient>("mykey");
        var instance1Copy = scope1.ServiceProvider.GetRequiredKeyedService<IDocumentExtractionClient>("mykey");
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<IDocumentExtractionClient>("mykey");

        var instance = Assert.IsType<SingletonMiddleware>(instance1);
        Assert.Same(instance, instance1Copy);
        Assert.Same(instance, instance2);
        Assert.IsType<TestDocumentExtractionClient>(instance.InnerClientPublic);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddDocumentExtractionClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        _ = lifetime.HasValue
            ? sc.AddDocumentExtractionClient(services => new TestDocumentExtractionClient(), lifetime.Value)
            : sc.AddDocumentExtractionClient(services => new TestDocumentExtractionClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IDocumentExtractionClient), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestDocumentExtractionClient>(sd.ImplementationFactory!(null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddKeyedDocumentExtractionClient_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        _ = lifetime.HasValue
            ? sc.AddKeyedDocumentExtractionClient("key", services => new TestDocumentExtractionClient(), lifetime.Value)
            : sc.AddKeyedDocumentExtractionClient("key", services => new TestDocumentExtractionClient());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IDocumentExtractionClient), sd.ServiceType);
        Assert.True(sd.IsKeyedService);
        Assert.Equal("key", sd.ServiceKey);
        Assert.Null(sd.KeyedImplementationInstance);
        Assert.NotNull(sd.KeyedImplementationFactory);
        Assert.IsType<TestDocumentExtractionClient>(sd.KeyedImplementationFactory!(null!, null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    public class SingletonMiddleware(IDocumentExtractionClient inner, IServiceProvider services) : DelegatingDocumentExtractionClient(inner)
    {
        public IDocumentExtractionClient InnerClientPublic => InnerClient;
        public IServiceProvider Services => services;
    }
}
