// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Tests;

public class HttpServiceEndpointResolverSharingTests
{
    [Fact]
    public void AddServiceDiscovery_HttpClient_SynchronousProviderDisposalDoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("test").AddServiceDiscovery();

        using var provider = services.BuildServiceProvider();
        using var handler = provider
            .GetRequiredService<IHttpMessageHandlerFactory>()
            .CreateHandler("test");

        Assert.NotNull(handler);
    }
}
