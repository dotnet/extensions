// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ServiceDiscovery.Http;
using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Tests;

/// <summary>
/// Tests that a single <see cref="HttpServiceEndpointResolver"/> is shared across HTTP message handlers
/// rather than created per handler build. Creating one per build leaks the resolver (an
/// <see cref="IAsyncDisposable"/> that roots refresh timers and configuration change-token subscriptions),
/// because <see cref="ResolvingHttpDelegatingHandler"/> never disposes it.
/// </summary>
public class HttpServiceEndpointResolverSharingTests
{
    [Fact]
    public async Task AddServiceDiscoveryCore_RegistersResolverAsSingleton()
    {
        await using var services = new ServiceCollection()
            .AddServiceDiscoveryCore()
            .BuildServiceProvider();

        var first = services.GetRequiredService<HttpServiceEndpointResolver>();
        var second = services.GetRequiredService<HttpServiceEndpointResolver>();

        Assert.Same(first, second);
    }

    [Fact]
    public async Task AddServiceDiscovery_HttpClient_HandlerUsesSharedResolverSingleton()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("test").AddServiceDiscovery();
        await using var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler("test");
        var resolvingHandler = FindHandler<ResolvingHttpDelegatingHandler>(handler);
        Assert.NotNull(resolvingHandler);

        var resolverField = typeof(ResolvingHttpDelegatingHandler)
            .GetField("_resolver", BindingFlags.Instance | BindingFlags.NonPublic);
        var usedResolver = resolverField!.GetValue(resolvingHandler);

        Assert.Same(provider.GetRequiredService<HttpServiceEndpointResolver>(), usedResolver);
    }

    private static T? FindHandler<T>(HttpMessageHandler handler)
        where T : HttpMessageHandler
    {
        for (var current = handler; current is not null; current = (current as DelegatingHandler)?.InnerHandler)
        {
            if (current is T match)
            {
                return match;
            }
        }

        return null;
    }
}
