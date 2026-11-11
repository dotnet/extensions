// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides extension methods for registering <see cref="ITextToSpeechClient"/> with a <see cref="IServiceCollection"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AITextToSpeech, UrlFormat = DiagnosticIds.UrlFormat)]
public static class TextToSpeechClientBuilderServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="ITextToSpeechClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClient">The inner <see cref="ITextToSpeechClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="TextToSpeechClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service.</remarks>
    public static TextToSpeechClientBuilder AddTextToSpeechClient(
        this IServiceCollection serviceCollection,
        ITextToSpeechClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClient);
        return AddTextToSpeechClient(serviceCollection, _ => innerClient, lifetime);
    }

    /// <summary>Registers a singleton <see cref="ITextToSpeechClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="ITextToSpeechClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="TextToSpeechClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service.</remarks>
    public static TextToSpeechClientBuilder AddTextToSpeechClient(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, ITextToSpeechClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new TextToSpeechClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(ITextToSpeechClient), builder.Build, lifetime));
        return builder;
    }

    /// <summary>Registers a keyed singleton <see cref="ITextToSpeechClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClient">The inner <see cref="ITextToSpeechClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="TextToSpeechClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service by default.</remarks>
    public static TextToSpeechClientBuilder AddKeyedTextToSpeechClient(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        ITextToSpeechClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClient);
        return AddKeyedTextToSpeechClient(serviceCollection, serviceKey, _ => innerClient, lifetime);
    }

    /// <summary>Registers a keyed singleton <see cref="ITextToSpeechClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="ITextToSpeechClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="TextToSpeechClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service by default.</remarks>
    public static TextToSpeechClientBuilder AddKeyedTextToSpeechClient(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        Func<IServiceProvider, ITextToSpeechClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new TextToSpeechClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(ITextToSpeechClient), serviceKey, factory: (services, serviceKey) => builder.Build(services), lifetime));
        return builder;
    }
}
