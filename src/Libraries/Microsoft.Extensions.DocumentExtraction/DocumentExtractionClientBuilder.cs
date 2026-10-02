// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>A builder for creating pipelines of <see cref="IDocumentExtractionClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DocumentExtractionClientBuilder
{
    private readonly Func<IServiceProvider, IDocumentExtractionClient> _innerClientFactory;

    /// <summary>The registered client factory instances.</summary>
    private List<Func<IDocumentExtractionClient, IServiceProvider, IDocumentExtractionClient>>? _clientFactories;

    /// <summary>Initializes a new instance of the <see cref="DocumentExtractionClientBuilder"/> class.</summary>
    /// <param name="innerClient">The inner <see cref="IDocumentExtractionClient"/> that represents the underlying backend.</param>
    public DocumentExtractionClientBuilder(IDocumentExtractionClient innerClient)
    {
        _ = Throw.IfNull(innerClient);
        _innerClientFactory = _ => innerClient;
    }

    /// <summary>Initializes a new instance of the <see cref="DocumentExtractionClientBuilder"/> class.</summary>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IDocumentExtractionClient"/> that represents the underlying backend.</param>
    public DocumentExtractionClientBuilder(Func<IServiceProvider, IDocumentExtractionClient> innerClientFactory)
    {
        _innerClientFactory = Throw.IfNull(innerClientFactory);
    }

    /// <summary>Builds an <see cref="IDocumentExtractionClient"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="IDocumentExtractionClient"/> instances.
    /// If null, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="IDocumentExtractionClient"/> that represents the entire pipeline.</returns>
    public IDocumentExtractionClient Build(IServiceProvider? services = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var documentExtractionClient = _innerClientFactory(services);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_clientFactories is not null)
        {
            for (var i = _clientFactories.Count - 1; i >= 0; i--)
            {
                documentExtractionClient = _clientFactories[i](documentExtractionClient, services) ??
                    throw new InvalidOperationException(
                        $"The {nameof(DocumentExtractionClientBuilder)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(IDocumentExtractionClient)} instances.");
            }
        }

        return documentExtractionClient;
    }

    /// <summary>Adds a factory for an intermediate OCR client to the OCR client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="DocumentExtractionClientBuilder"/> instance.</returns>
    public DocumentExtractionClientBuilder Use(Func<IDocumentExtractionClient, IDocumentExtractionClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        return Use((innerClient, _) => clientFactory(innerClient));
    }

    /// <summary>Adds a factory for an intermediate OCR client to the OCR client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="DocumentExtractionClientBuilder"/> instance.</returns>
    public DocumentExtractionClientBuilder Use(Func<IDocumentExtractionClient, IServiceProvider, IDocumentExtractionClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        (_clientFactories ??= []).Add(clientFactory);
        return this;
    }
}
