// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DocumentExtraction;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides dependency-injection registration methods for extraction clients.
/// </summary>
public static class DocumentExtractionClientBuilderServiceCollectionExtensions
{
    /// <summary>
    /// Registers a document extraction client and returns its middleware builder.
    /// </summary>
    /// <remarks>
    /// The supplied inner client is shared by all resolutions, including scoped or transient pipelines.
    /// Disposing one pipeline disposes that inner client. Use the factory overload to create independent
    /// clients for scoped or transient lifetimes.
    /// </remarks>
    /// <returns>The middleware builder.</returns>
    public static DocumentExtractionClientBuilder AddDocumentExtractionClient(
        this IServiceCollection services,
        IDocumentExtractionClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(innerClient);
        return AddDocumentExtractionClient(services, _ => innerClient, lifetime);
    }

    /// <summary>
    /// Registers a document extraction client factory and returns its middleware builder.
    /// </summary>
    /// <returns>The middleware builder.</returns>
    public static DocumentExtractionClientBuilder AddDocumentExtractionClient(
        this IServiceCollection services,
        Func<IServiceProvider, IDocumentExtractionClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(innerClientFactory);

        DocumentExtractionClientBuilder builder = new(innerClientFactory);
        services.Add(new ServiceDescriptor(
            typeof(IDocumentExtractionClient),
            serviceProvider => builder.Build(serviceProvider),
            lifetime));
        return builder;
    }

    /// <summary>
    /// Registers a keyed document extraction client factory and returns its middleware builder.
    /// </summary>
    /// <returns>The middleware builder.</returns>
    public static DocumentExtractionClientBuilder AddKeyedDocumentExtractionClient(
        this IServiceCollection services,
        object? serviceKey,
        Func<IServiceProvider, IDocumentExtractionClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(innerClientFactory);

        DocumentExtractionClientBuilder builder = new(innerClientFactory);
        services.Add(new ServiceDescriptor(
            typeof(IDocumentExtractionClient),
            serviceKey,
            (serviceProvider, _) => builder.Build(serviceProvider),
            lifetime));
        return builder;
    }
}
