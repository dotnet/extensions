// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Shared.Diagnostics;

#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI.Contents;

/// <summary>
/// Provides a global registry for custom AI content types and their
/// discriminator IDs for use in System.Text.Json polymorphic serialization.
/// </summary>
public static class AIContentRegistry
{
    private static readonly ConcurrentDictionary<Type, (string DiscriminatorId, IJsonTypeInfoResolver? Resolver)> _registry = new();
    private static readonly Dictionary<string, Type> _discriminatorIdToType = typeof(AIContent)
        .GetCustomAttributes<JsonDerivedTypeAttribute>()
        .ToDictionary(attr => (string)attr.TypeDiscriminator!, attr => attr.DerivedType);

    /// <summary>
    /// Registers a custom AI content type with a discriminator ID.
    /// </summary>
    /// <typeparam name="TContent">The custom content type to be generated.</typeparam>
    /// <param name="typeDiscriminatorId">The type discriminator associated with the type.</param>
    /// <param name="resolver">The contract resolver used for the specified derived type.</param>
    public static void RegisterCustomAIContentType<TContent>(string typeDiscriminatorId, IJsonTypeInfoResolver? resolver = null)
        where TContent : AIContent
    {
        _ = Throw.IfNull(typeDiscriminatorId);
        RegisterCore(typeof(TContent), typeDiscriminatorId, resolver);
    }

    /// <summary>
    /// Registers a custom AI content type with a discriminator ID.
    /// </summary>
    /// <param name="contentType">The custom content type to be generated.</param>
    /// <param name="typeDiscriminatorId">The type discriminator associated with the type.</param>
    /// <param name="resolver">The contract resolver used for the specified derived type.</param>
    public static void RegisterCustomAIContentType(Type contentType, string typeDiscriminatorId, IJsonTypeInfoResolver? resolver = null)
    {
        _ = Throw.IfNull(contentType);
        _ = Throw.IfNull(typeDiscriminatorId);

        if (!typeof(AIContent).IsAssignableFrom(contentType))
        {
            Throw.ArgumentException(nameof(contentType), "The content type must derive from AIContent.");
        }

        RegisterCore(contentType, typeDiscriminatorId, resolver);
    }

    /// <summary>
    /// Creates a <see cref="IJsonTypeInfoResolver"/> wrapper that applies the configuration of the registry over the specified resolver.
    /// </summary>
    /// <param name="resolver">The underlying resolver over which to apply configuration from the registry.</param>
    /// <returns>A new <see cref="IJsonTypeInfoResolver"/> that applies the configuration from the registry.</returns>
    public static IJsonTypeInfoResolver ApplyAIContentRegistry(this IJsonTypeInfoResolver resolver)
    {
        _ = Throw.IfNull(resolver);
        return new AIContentRegistryResolver(resolver);
    }

    private static void RegisterCore(Type contentType, string typeDiscriminatorId, IJsonTypeInfoResolver? resolver)
    {
        if (contentType.Assembly == typeof(AIContent).Assembly)
        {
            Throw.ArgumentException(nameof(contentType), "Cannot register built-in AI content types.");
        }

        ValidateConfiguration(contentType, typeDiscriminatorId, resolver, out bool alreadyRegistered);
        if (alreadyRegistered)
        {
            return;
        }

        lock (_registry)
        {
            ValidateConfiguration(contentType, typeDiscriminatorId, resolver, out alreadyRegistered);
            if (alreadyRegistered)
            {
                return;
            }

            bool success = _registry.TryAdd(contentType, (typeDiscriminatorId, resolver));
            _discriminatorIdToType.Add(typeDiscriminatorId, contentType);
            Debug.Assert(success, "must not conflict with other entries.");
        }

        static void ValidateConfiguration(Type contentType, string typeDiscriminatorId, IJsonTypeInfoResolver? resolver, out bool alreadyRegistered)
        {
            alreadyRegistered = false;
            if (_registry.TryGetValue(contentType, out var existing))
            {
                if (existing == (typeDiscriminatorId, resolver))
                {
                    // We have an equivalent registration, return early.
                    alreadyRegistered = true;
                    return;
                }

                throw new InvalidOperationException($"The content type '{contentType.FullName}' has already been registered with conflicting configuration.");
            }

            if (_discriminatorIdToType.TryGetValue(typeDiscriminatorId, out Type? existingType))
            {
                throw new InvalidOperationException($"The discriminator ID '{typeDiscriminatorId}' conflicts with that of '{existingType}'.");
            }
        }
    }

    private sealed class AIContentRegistryResolver(IJsonTypeInfoResolver underlying) : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo? typeInfo = GetTypeInfoCore(type, options);

            if (typeInfo is not null && typeInfo.Type == typeof(AIContent))
            {
                ModifyAIContentTypeInfo(typeInfo);
            }

            return typeInfo;
        }

        private JsonTypeInfo? GetTypeInfoCore(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo? typeInfo = underlying.GetTypeInfo(type, options);
            if (typeInfo is not null)
            {
                return typeInfo;
            }

            foreach (var kvp in _registry)
            {
                if (kvp.Value.Resolver is { } resolver)
                {
                    typeInfo = resolver.GetTypeInfo(type, options);
                    if (typeInfo is not null)
                    {
                        return typeInfo;
                    }
                }
            }

            return null;
        }

        private static void ModifyAIContentTypeInfo(JsonTypeInfo typeInfo)
        {
            Debug.Assert(typeInfo.Type == typeof(AIContent), "Should only be used for AIContent types.");
            if (typeInfo.PolymorphismOptions is null)
            {
                Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.None, "A custom converter should have been applied for the type.");
                return;
            }

            foreach (var entry in _registry)
            {
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new(entry.Key, entry.Value.DiscriminatorId));
            }
        }
    }
}
