// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="ITextToImageClient"/>.</summary>
[Experimental("MEAI001")]
public sealed class TextToImageClientBuilder
{
    private readonly Func<IServiceProvider, ITextToImageClient> _innerClientFactory;

    /// <summary>The registered client factory instances.</summary>
    private List<Func<ITextToImageClient, IServiceProvider, ITextToImageClient>>? _clientFactories;

    /// <summary>Initializes a new instance of the <see cref="TextToImageClientBuilder"/> class.</summary>
    /// <param name="innerClient">The inner <see cref="ITextToImageClient"/> that represents the underlying backend.</param>
    public TextToImageClientBuilder(ITextToImageClient innerClient)
    {
        _ = Throw.IfNull(innerClient);
        _innerClientFactory = _ => innerClient;
    }

    /// <summary>Initializes a new instance of the <see cref="TextToImageClientBuilder"/> class.</summary>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="ITextToImageClient"/> that represents the underlying backend.</param>
    public TextToImageClientBuilder(Func<IServiceProvider, ITextToImageClient> innerClientFactory)
    {
        _innerClientFactory = Throw.IfNull(innerClientFactory);
    }

    /// <summary>Builds an <see cref="ITextToImageClient"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="ITextToImageClient"/> instances.
    /// If null, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="ITextToImageClient"/> that represents the entire pipeline.</returns>
    public ITextToImageClient Build(IServiceProvider? services = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var textToImageClient = _innerClientFactory(services);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_clientFactories is not null)
        {
            for (var i = _clientFactories.Count - 1; i >= 0; i--)
            {
                textToImageClient = _clientFactories[i](textToImageClient, services) ??
                    throw new InvalidOperationException(
                        $"The {nameof(TextToImageClientBuilder)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(ITextToImageClient)} instances.");
            }
        }

        return textToImageClient;
    }

    /// <summary>Adds a factory for an intermediate text to image client to the text to image client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="TextToImageClientBuilder"/> instance.</returns>
    public TextToImageClientBuilder Use(Func<ITextToImageClient, ITextToImageClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        return Use((innerClient, _) => clientFactory(innerClient));
    }

    /// <summary>Adds a factory for an intermediate text to image client to the text to image client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="TextToImageClientBuilder"/> instance.</returns>
    public TextToImageClientBuilder Use(Func<ITextToImageClient, IServiceProvider, ITextToImageClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        (_clientFactories ??= []).Add(clientFactory);
        return this;
    }
}
