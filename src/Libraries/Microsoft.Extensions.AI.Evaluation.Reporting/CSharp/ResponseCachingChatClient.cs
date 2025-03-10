// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

/// <summary>
/// An <see cref="IChatClient"/> that wraps another <see cref="IChatClient"/> and caches all responses generated using
/// the wrapped <see cref="IChatClient"/> in the supplied <see cref="IDistributedCache"/>.
/// </summary>
public sealed class ResponseCachingChatClient : DistributedCachingChatClient
{
    private readonly IReadOnlyList<string> _cachingKeys;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseCachingChatClient"/> class that wraps the supplied
    /// <paramref name="originalChatClient"/> and caches all responses generated using
    /// <paramref name="originalChatClient"/> in the supplied <paramref name="cache"/>.
    /// </summary>
    /// <param name="originalChatClient">The <see cref="IChatClient"/> that is wrapped.</param>
    /// <param name="cache">The <see cref="IDistributedCache"/> where the cached responses are stored.</param>
    /// <param name="cachingKeys">
    /// A collection of unique strings that should be hashed when generating the cache keys for cached AI responses.
    /// See <see cref="ReportingConfiguration.CachingKeys"/> for more information about this concept.
    /// </param>
    public ResponseCachingChatClient(
        IChatClient originalChatClient,
        IDistributedCache cache,
        IEnumerable<string> cachingKeys)
            : base(originalChatClient, cache)
    {
        _cachingKeys = [.. cachingKeys];
    }

    /// <inheritdoc/>
    protected override string GetCacheKey(params ReadOnlySpan<object?> values)
        => base.GetCacheKey([.. values, .. _cachingKeys]);

}
