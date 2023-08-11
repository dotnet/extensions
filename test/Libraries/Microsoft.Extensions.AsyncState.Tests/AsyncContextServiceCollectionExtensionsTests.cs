// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
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
}
