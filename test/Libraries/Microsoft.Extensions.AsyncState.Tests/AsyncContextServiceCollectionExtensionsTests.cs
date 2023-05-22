// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Microsoft.Extensions.AsyncState.Test;

public class AsyncContextServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAsyncStateCore_Throws_WhenNullService()
    {
        Assert.Throws<ArgumentNullException>(() => AsyncStateExtensions.AddAsyncStateCore(null!));
    }

    [Fact]
    public void AddAsyncStateCore_AddsWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAsyncStateCore();

        // Assert
        var serviceDescriptor = services.First(x => x.ServiceType == typeof(IAsyncContext<>));
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);

        serviceDescriptor = services.First(x => x.ServiceType == typeof(IAsyncState));
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);

        serviceDescriptor = services.First(x => x.ServiceType == typeof(IAsyncLocalContext<>));
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void TryRemoveAsyncStateCore_Throws_WhenNullService()
    {
        Assert.Throws<ArgumentNullException>(() => AsyncStateExtensions.TryRemoveAsyncStateCore(null!));
    }

    [Fact]
    public void TryRemoveAsyncStateCore_RemovesAsyncContext()
    {
        var services = new ServiceCollection();

        services.AddAsyncStateCore();

        Assert.NotNull(services.FirstOrDefault(x =>
            (x.ServiceType == typeof(IAsyncContext<>)) && (x.ImplementationType == typeof(AsyncContext<>))));

        services.TryRemoveAsyncStateCore();

        Assert.Null(services.FirstOrDefault(x =>
            (x.ServiceType == typeof(IAsyncContext<>)) && (x.ImplementationType == typeof(AsyncContext<>))));
    }

    [Fact]
    public void TryRemoveSingleton_DoesNothingToEmptyServices()
    {
        var services = new ServiceCollection();

        services.TryRemoveSingleton(typeof(IThing), typeof(Thing));

        Assert.Empty(services);
    }

    [Fact]
    public void TryRemoveSingleton_RemovesWhenPresent()
    {
        var services = new ServiceCollection();

        services.TryAddSingleton<IThing, Thing>();

        Assert.Single(services);
        var descriptor = services[0];
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(IThing), descriptor.ServiceType);
        Assert.Equal(typeof(Thing), descriptor.ImplementationType);

        services.TryRemoveSingleton(typeof(IThing), typeof(Thing));

        Assert.Empty(services);
    }

    [Fact]
    public void TryRemoveSingleton_DoesNotRemoveOtherThanSpecified()
    {
        var services = new ServiceCollection();

        services.TryAddSingleton<IThing, Thing>();

        Assert.Single(services);
        var descriptor = services[0];
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(IThing), descriptor.ServiceType);
        Assert.Equal(typeof(Thing), descriptor.ImplementationType);

        services.TryRemoveSingleton(typeof(IThing), typeof(AnotherThing));

        Assert.Single(services);
        var descriptor2 = services[0];
        Assert.Equal(ServiceLifetime.Singleton, descriptor2.Lifetime);
        Assert.Equal(typeof(IThing), descriptor2.ServiceType);
        Assert.Equal(typeof(Thing), descriptor2.ImplementationType);
    }
}
