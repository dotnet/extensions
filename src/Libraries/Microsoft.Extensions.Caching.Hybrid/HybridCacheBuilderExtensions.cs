// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Configuration extension methods for <see cref="IHybridCacheBuilder"/> / <see cref="HybridCache"/>.
/// </summary>
public static class HybridCacheBuilderExtensions
{
    /// <summary>
    /// Serialize values of type <typeparamref name="T"/> with the specified serializer from <paramref name="serializer"/>.
    /// </summary>
    /// <typeparam name="T">The type to be serialized.</typeparam>
    /// <returns>The <see cref="IHybridCacheBuilder"/> instance.</returns>
    public static IHybridCacheBuilder AddSerializer<T>(this IHybridCacheBuilder builder, IHybridCacheSerializer<T> serializer)
    {
        _ = Throw.IfNull(builder).Services.AddSingleton<IHybridCacheSerializer<T>>(serializer);
        return builder;
    }

    /// <summary>
    /// Serialize values of type <typeparamref name="T"/> with the serializer of type <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="T">The type to be serialized.</typeparam>
    /// <typeparam name="TImplementation">The serializer to use for this type.</typeparam>
    /// <returns>The <see cref="IHybridCacheBuilder"/> instance.</returns>
    public static IHybridCacheBuilder AddSerializer<T,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IHybridCacheBuilder builder)
        where TImplementation : class, IHybridCacheSerializer<T>
    {
        _ = Throw.IfNull(builder).Services.AddSingleton<IHybridCacheSerializer<T>, TImplementation>();
        return builder;
    }

    /// <summary>
    /// Add <paramref name="factory"/> as an additional serializer factory, which can provide serializers for multiple types.
    /// </summary>
    /// <returns>The <see cref="IHybridCacheBuilder"/> instance.</returns>
    public static IHybridCacheBuilder AddSerializerFactory(this IHybridCacheBuilder builder, IHybridCacheSerializerFactory factory)
    {
        _ = Throw.IfNull(builder).Services.AddSingleton<IHybridCacheSerializerFactory>(factory);
        return builder;
    }

    /// <summary>
    /// Add a factory of type <typeparamref name="TImplementation"/> as an additional serializer factory, which can provide serializers for multiple types.
    /// </summary>
    /// <typeparam name="TImplementation">The type of the serializer factory.</typeparam>
    /// <returns>The <see cref="IHybridCacheBuilder"/> instance.</returns>
    public static IHybridCacheBuilder AddSerializerFactory<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IHybridCacheBuilder builder)
        where TImplementation : class, IHybridCacheSerializerFactory
    {
        _ = Throw.IfNull(builder).Services.AddSingleton<IHybridCacheSerializerFactory, TImplementation>();
        return builder;
    }
}
