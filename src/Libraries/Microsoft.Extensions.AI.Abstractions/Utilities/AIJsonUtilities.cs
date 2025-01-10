// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S1121 // Assignments should not be made from within sub-expressions

namespace Microsoft.Extensions.AI;

public static partial class AIJsonUtilities
{
    /// <summary>
    /// Adds a custom content type to the polymorphic configuration for <see cref="AIContent"/>.
    /// </summary>
    /// <typeparam name="TContent">The custom content type to configure.</typeparam>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="typeDiscriminatorId">The type discriminator id for the content type.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> or <paramref name="typeDiscriminatorId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="TContent"/> is a built-in content type.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="options"/> is a read-only instance.</exception>
    public static void AddAIContentType<TContent>(this JsonSerializerOptions options, string typeDiscriminatorId)
        where TContent : AIContent
    {
        _ = Throw.IfNull(options);
        _ = Throw.IfNull(typeDiscriminatorId);

        AddAIContentTypeCore(options, typeof(TContent), typeDiscriminatorId);
    }

    /// <summary>
    /// Adds a custom content type to the polymorphic configuration for <see cref="AIContent"/>.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="contentType">The custom content type to configure.</param>
    /// <param name="typeDiscriminatorId">The type discriminator id for the content type.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/>, <paramref name="contentType"/>, or <paramref name="typeDiscriminatorId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="contentType"/> is a built-in content type or does not derived from <see cref="AIContent"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="options"/> is a read-only instance.</exception>
    public static void AddAIContentType(this JsonSerializerOptions options, Type contentType, string typeDiscriminatorId)
    {
        _ = Throw.IfNull(options);
        _ = Throw.IfNull(contentType);
        _ = Throw.IfNull(typeDiscriminatorId);

        if (!typeof(AIContent).IsAssignableFrom(contentType))
        {
            Throw.ArgumentException(nameof(contentType), "The content type must derive from AIContent.");
        }

        AddAIContentTypeCore(options, contentType, typeDiscriminatorId);
    }

    private static void AddAIContentTypeCore(JsonSerializerOptions options, Type contentType, string typeDiscriminatorId)
    {
        if (contentType.Assembly == typeof(AIContent).Assembly)
        {
            Throw.ArgumentException(nameof(contentType), "Cannot register built-in AI content types.");
        }

        IJsonTypeInfoResolver resolver = options.TypeInfoResolver ?? DefaultOptions.TypeInfoResolver!;
        options.TypeInfoResolver = resolver.WithAddedModifier(typeInfo =>
        {
            if (typeInfo.Type == typeof(AIContent))
            {
                (typeInfo.PolymorphismOptions ??= new()).DerivedTypes.Add(new(contentType, typeDiscriminatorId));
            }
        });
    }
}
