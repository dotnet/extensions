// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="ITextToSpeechClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AITextToSpeech, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class TextToSpeechClientBuilder
{
    private readonly Func<IServiceProvider, ITextToSpeechClient> _innerClientFactory;

    /// <summary>The registered client factory instances.</summary>
    private List<Func<ITextToSpeechClient, IServiceProvider, ITextToSpeechClient>>? _clientFactories;

    /// <summary>Initializes a new instance of the <see cref="TextToSpeechClientBuilder"/> class.</summary>
    /// <param name="innerClient">The inner <see cref="ITextToSpeechClient"/> that represents the underlying backend.</param>
    public TextToSpeechClientBuilder(ITextToSpeechClient innerClient)
    {
        _ = Throw.IfNull(innerClient);
        _innerClientFactory = _ => innerClient;
    }

    /// <summary>Initializes a new instance of the <see cref="TextToSpeechClientBuilder"/> class.</summary>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="ITextToSpeechClient"/> that represents the underlying backend.</param>
    public TextToSpeechClientBuilder(Func<IServiceProvider, ITextToSpeechClient> innerClientFactory)
    {
        _innerClientFactory = Throw.IfNull(innerClientFactory);
    }

    /// <summary>Builds an <see cref="ITextToSpeechClient"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="ITextToSpeechClient"/> instances.
    /// If null, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="ITextToSpeechClient"/> that represents the entire pipeline.</returns>
    public ITextToSpeechClient Build(IServiceProvider? services = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var client = _innerClientFactory(services);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_clientFactories is not null)
        {
            for (var i = _clientFactories.Count - 1; i >= 0; i--)
            {
                client = _clientFactories[i](client, services) ??
                    throw new InvalidOperationException(
                        $"The {nameof(TextToSpeechClientBuilder)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(ITextToSpeechClient)} instances.");
            }
        }

        return client;
    }

    /// <summary>Adds a factory for an intermediate text-to-speech client to the text-to-speech client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="TextToSpeechClientBuilder"/> instance.</returns>
    public TextToSpeechClientBuilder Use(Func<ITextToSpeechClient, ITextToSpeechClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        return Use((innerClient, _) => clientFactory(innerClient));
    }

    /// <summary>Adds a factory for an intermediate text-to-speech client to the text-to-speech client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="TextToSpeechClientBuilder"/> instance.</returns>
    public TextToSpeechClientBuilder Use(Func<ITextToSpeechClient, IServiceProvider, ITextToSpeechClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        (_clientFactories ??= []).Add(clientFactory);
        return this;
    }
}
