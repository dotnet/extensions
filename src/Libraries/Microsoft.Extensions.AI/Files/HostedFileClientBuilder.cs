// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IHostedFileClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class HostedFileClientBuilder
{
    private readonly Func<IServiceProvider, IHostedFileClient> _innerClientFactory;

    /// <summary>The registered client factory instances.</summary>
    private List<Func<IHostedFileClient, IServiceProvider, IHostedFileClient>>? _clientFactories;

    /// <summary>Initializes a new instance of the <see cref="HostedFileClientBuilder"/> class.</summary>
    /// <param name="innerClient">The inner <see cref="IHostedFileClient"/> that represents the underlying backend.</param>
    public HostedFileClientBuilder(IHostedFileClient innerClient)
    {
        _ = Throw.IfNull(innerClient);
        _innerClientFactory = _ => innerClient;
    }

    /// <summary>Initializes a new instance of the <see cref="HostedFileClientBuilder"/> class.</summary>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IHostedFileClient"/> that represents the underlying backend.</param>
    public HostedFileClientBuilder(Func<IServiceProvider, IHostedFileClient> innerClientFactory)
    {
        _innerClientFactory = Throw.IfNull(innerClientFactory);
    }

    /// <summary>Builds an <see cref="IHostedFileClient"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="IHostedFileClient"/> instances.
    /// If null, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="IHostedFileClient"/> that represents the entire pipeline.</returns>
    public IHostedFileClient Build(IServiceProvider? services = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var fileClient = _innerClientFactory(services);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_clientFactories is not null)
        {
            for (var i = _clientFactories.Count - 1; i >= 0; i--)
            {
                fileClient = _clientFactories[i](fileClient, services) ??
                    throw new InvalidOperationException(
                        $"The {nameof(HostedFileClientBuilder)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(IHostedFileClient)} instances.");
            }
        }

        return fileClient;
    }

    /// <summary>Adds a factory for an intermediate hosted file client to the hosted file client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="HostedFileClientBuilder"/> instance.</returns>
    public HostedFileClientBuilder Use(Func<IHostedFileClient, IHostedFileClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        return Use((innerClient, _) => clientFactory(innerClient));
    }

    /// <summary>Adds a factory for an intermediate hosted file client to the hosted file client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="HostedFileClientBuilder"/> instance.</returns>
    public HostedFileClientBuilder Use(Func<IHostedFileClient, IServiceProvider, IHostedFileClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        (_clientFactories ??= []).Add(clientFactory);
        return this;
    }
}
