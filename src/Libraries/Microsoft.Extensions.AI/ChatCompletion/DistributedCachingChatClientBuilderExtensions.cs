// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Extension methods for adding a <see cref="DistributedCachingChatClient"/> to an <see cref="IChatClient"/> pipeline.
/// </summary>
public static class DistributedCachingChatClientBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="DistributedCachingChatClient"/> as the next stage in the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <param name="storage">
    /// An optional <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache. If not supplied, an instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="DistributedCachingChatClient"/> instance.</param>
    /// <returns>The <see cref="ChatClientBuilder"/> provided as <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The <see cref="DistributedCachingChatClient"/> employs JSON serialization as part of storing the cached data. It is not guaranteed that
    /// the object models used by <see cref="ChatMessage"/>, <see cref="ChatOptions"/>, <see cref="ChatResponse"/>, <see cref="ChatResponseUpdate"/>,
    /// or any of the other objects in the chat client pipeline will roundtrip through JSON serialization with full fidelity. For example,
    /// <see cref="ChatMessage.RawRepresentation"/> will be ignored, and <see cref="object"/> values in <see cref="ChatMessage.AdditionalProperties"/>
    /// will deserialize as <see cref="JsonElement"/> rather than as the original type. In general, code using <see cref="DistributedCachingChatClient"/>
    /// should only rely on accessing data that can be preserved well enough through JSON serialization and deserialization.
    /// </remarks>
    public static ChatClientBuilder UseDistributedCache(this ChatClientBuilder builder, IDistributedCache? storage = null, Action<DistributedCachingChatClient>? configure = null)
    {
        _ = Throw.IfNull(builder);
        return builder.Use((innerClient, services) =>
        {
            storage ??= services.GetRequiredService<IDistributedCache>();
            var chatClient = new DistributedCachingChatClient(innerClient, storage);
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }
}
