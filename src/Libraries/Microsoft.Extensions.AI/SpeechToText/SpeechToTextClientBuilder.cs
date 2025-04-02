// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="ISpeechToTextClient"/>.</summary>
[Experimental("MEAI001")]
public sealed class SpeechToTextClientBuilder
{
    private readonly Func<IServiceProvider, ISpeechToTextClient> _innerClientFactory;

    /// <summary>The registered client factory instances.</summary>
    private List<Func<ISpeechToTextClient, IServiceProvider, ISpeechToTextClient>>? _clientFactories;

    /// <summary>Initializes a new instance of the <see cref="SpeechToTextClientBuilder"/> class.</summary>
    /// <param name="innerClient">The inner <see cref="ISpeechToTextClient"/> that represents the underlying backend.</param>
    public SpeechToTextClientBuilder(ISpeechToTextClient innerClient)
    {
        _ = Throw.IfNull(innerClient);
        _innerClientFactory = _ => innerClient;
    }

    /// <summary>Initializes a new instance of the <see cref="SpeechToTextClientBuilder"/> class.</summary>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="ISpeechToTextClient"/> that represents the underlying backend.</param>
    public SpeechToTextClientBuilder(Func<IServiceProvider, ISpeechToTextClient> innerClientFactory)
    {
        _innerClientFactory = Throw.IfNull(innerClientFactory);
    }

    /// <summary>Builds an <see cref="ISpeechToTextClient"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="ISpeechToTextClient"/> instances.
    /// If null, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="ISpeechToTextClient"/> that represents the entire pipeline.</returns>
    public ISpeechToTextClient Build(IServiceProvider? services = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var audioClient = _innerClientFactory(services);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_clientFactories is not null)
        {
            for (var i = _clientFactories.Count - 1; i >= 0; i--)
            {
                audioClient = _clientFactories[i](audioClient, services) ??
                    throw new InvalidOperationException(
                        $"The {nameof(SpeechToTextClientBuilder)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(ISpeechToTextClient)} instances.");
            }
        }

        return audioClient;
    }

    /// <summary>Adds a factory for an intermediate audio transcription client to the audio transcription client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="SpeechToTextClientBuilder"/> instance.</returns>
    public SpeechToTextClientBuilder Use(Func<ISpeechToTextClient, ISpeechToTextClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        return Use((innerClient, _) => clientFactory(innerClient));
    }

    /// <summary>Adds a factory for an intermediate audio transcription client to the audio transcription client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="SpeechToTextClientBuilder"/> instance.</returns>
    public SpeechToTextClientBuilder Use(Func<ISpeechToTextClient, IServiceProvider, ISpeechToTextClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        (_clientFactories ??= []).Add(clientFactory);
        return this;
    }
}
