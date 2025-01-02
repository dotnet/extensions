// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
