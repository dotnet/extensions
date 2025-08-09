// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IImageClient"/>.</summary>
[Experimental("MEAI001")]
public sealed class ImageClientBuilder
{
    private readonly Func<IServiceProvider, IImageClient> _innerClientFactory;

    /// <summary>The registered client factory instances.</summary>
    private List<Func<IImageClient, IServiceProvider, IImageClient>>? _clientFactories;

    /// <summary>Initializes a new instance of the <see cref="ImageClientBuilder"/> class.</summary>
    /// <param name="innerClient">The inner <see cref="IImageClient"/> that represents the underlying backend.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    public ImageClientBuilder(IImageClient innerClient)
    {
        _ = Throw.IfNull(innerClient);
        _innerClientFactory = _ => innerClient;
    }

    /// <summary>Initializes a new instance of the <see cref="ImageClientBuilder"/> class.</summary>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IImageClient"/> that represents the underlying backend.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClientFactory"/> is <see langword="null"/>.</exception>
    public ImageClientBuilder(Func<IServiceProvider, IImageClient> innerClientFactory)
    {
        _innerClientFactory = Throw.IfNull(innerClientFactory);
    }

    /// <summary>Builds an <see cref="IImageClient"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="IImageClient"/> instances.
    /// If null, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="IImageClient"/> that represents the entire pipeline.</returns>
    public IImageClient Build(IServiceProvider? services = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var imageClient = _innerClientFactory(services);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_clientFactories is not null)
        {
            for (var i = _clientFactories.Count - 1; i >= 0; i--)
            {
                imageClient = _clientFactories[i](imageClient, services) ??
                    throw new InvalidOperationException(
                        $"The {nameof(ImageClientBuilder)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(IImageClient)} instances.");
            }
        }

        return imageClient;
    }

    /// <summary>Adds a factory for an intermediate text to image client to the text to image client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="ImageClientBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="clientFactory"/> is <see langword="null"/>.</exception>
    public ImageClientBuilder Use(Func<IImageClient, IImageClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        return Use((innerClient, _) => clientFactory(innerClient));
    }

    /// <summary>Adds a factory for an intermediate text to image client to the text to image client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="ImageClientBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="clientFactory"/> is <see langword="null"/>.</exception>
    public ImageClientBuilder Use(Func<IImageClient, IServiceProvider, IImageClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        (_clientFactories ??= []).Add(clientFactory);
        return this;
    }
}
