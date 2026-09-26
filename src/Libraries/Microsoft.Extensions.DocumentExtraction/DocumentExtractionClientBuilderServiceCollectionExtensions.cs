// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides extension methods for registering <see cref="IDocumentExtractionClient"/> with a <see cref="IServiceCollection"/>.</summary>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
public static class DocumentExtractionClientBuilderServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="IDocumentExtractionClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClient">The inner <see cref="IDocumentExtractionClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="DocumentExtractionClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    public static DocumentExtractionClientBuilder AddDocumentExtractionClient(
        this IServiceCollection serviceCollection,
        IDocumentExtractionClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddDocumentExtractionClient(serviceCollection, _ => innerClient, lifetime);

    /// <summary>Registers a singleton <see cref="IDocumentExtractionClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IDocumentExtractionClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="DocumentExtractionClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="innerClientFactory"/> is <see langword="null"/>.</exception>
    public static DocumentExtractionClientBuilder AddDocumentExtractionClient(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, IDocumentExtractionClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new DocumentExtractionClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IDocumentExtractionClient), builder.Build, lifetime));
        return builder;
    }

    /// <summary>Registers a keyed singleton <see cref="IDocumentExtractionClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClient">The inner <see cref="IDocumentExtractionClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="DocumentExtractionClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    public static DocumentExtractionClientBuilder AddKeyedDocumentExtractionClient(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        IDocumentExtractionClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        => AddKeyedDocumentExtractionClient(serviceCollection, serviceKey, _ => innerClient, lifetime);

    /// <summary>Registers a keyed singleton <see cref="IDocumentExtractionClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IDocumentExtractionClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="DocumentExtractionClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="innerClientFactory"/> is <see langword="null"/>.</exception>
    public static DocumentExtractionClientBuilder AddKeyedDocumentExtractionClient(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        Func<IServiceProvider, IDocumentExtractionClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new DocumentExtractionClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IDocumentExtractionClient), serviceKey, factory: (services, serviceKey) => builder.Build(services), lifetime));
        return builder;
    }
}
