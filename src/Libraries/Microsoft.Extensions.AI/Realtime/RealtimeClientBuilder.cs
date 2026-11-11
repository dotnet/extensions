// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IRealtimeClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class RealtimeClientBuilder
{
    private readonly Func<IServiceProvider, IRealtimeClient> _innerClientFactory;

    /// <summary>The registered client factory instances.</summary>
    private List<Func<IRealtimeClient, IServiceProvider, IRealtimeClient>>? _clientFactories;

    /// <summary>Initializes a new instance of the <see cref="RealtimeClientBuilder"/> class.</summary>
    /// <param name="innerClient">The inner <see cref="IRealtimeClient"/> that represents the underlying backend.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    public RealtimeClientBuilder(IRealtimeClient innerClient)
    {
        _ = Throw.IfNull(innerClient);
        _innerClientFactory = _ => innerClient;
    }

    /// <summary>Initializes a new instance of the <see cref="RealtimeClientBuilder"/> class.</summary>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IRealtimeClient"/> that represents the underlying backend.</param>
    public RealtimeClientBuilder(Func<IServiceProvider, IRealtimeClient> innerClientFactory)
    {
        _innerClientFactory = Throw.IfNull(innerClientFactory);
    }

    /// <summary>Builds an <see cref="IRealtimeClient"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="IRealtimeClient"/> instances.
    /// If <see langword="null"/>, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="IRealtimeClient"/> that represents the entire pipeline.</returns>
    public IRealtimeClient Build(IServiceProvider? services = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var client = _innerClientFactory(services);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_clientFactories is not null)
        {
            for (var i = _clientFactories.Count - 1; i >= 0; i--)
            {
                client = _clientFactories[i](client, services);
                if (client is null)
                {
                    Throw.InvalidOperationException(
                        $"The {nameof(RealtimeClientBuilder)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(IRealtimeClient)} instances.");
                }
            }
        }

        return client;
    }

    /// <summary>Adds a factory for an intermediate realtime client to the realtime client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="RealtimeClientBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="clientFactory"/> is <see langword="null"/>.</exception>
    public RealtimeClientBuilder Use(Func<IRealtimeClient, IRealtimeClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        return Use((innerClient, _) => clientFactory(innerClient));
    }

    /// <summary>Adds a factory for an intermediate realtime client to the realtime client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="RealtimeClientBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="clientFactory"/> is <see langword="null"/>.</exception>
    public RealtimeClientBuilder Use(Func<IRealtimeClient, IServiceProvider, IRealtimeClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        (_clientFactories ??= []).Add(clientFactory);
        return this;
    }

}
