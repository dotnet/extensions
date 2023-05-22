// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AsyncState;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.AsyncState.Test;

public class AsyncStateHttpContextExtensionsTests
{
    [Fact]
    public void AddAsyncStateHttpContext_Throws_WhenNullService()
    {
        Assert.Throws<ArgumentNullException>(() => AsyncStateHttpContextExtensions.AddAsyncStateHttpContext(null!));
    }

    [Fact]
    public void AddAsyncStateHttpContext_AddsWithCorrectLifetime()
    {
        var services = new ServiceCollection();

        services.AddAsyncStateHttpContext();

        var serviceDescriptor = services.First(x => x.ServiceType == typeof(IHttpContextAccessor));
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);

        serviceDescriptor = services.First(x => x.ServiceType == typeof(IAsyncContext<>));
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
    }
}
