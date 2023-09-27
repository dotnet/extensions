// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Net.Http;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal sealed class ByAuthorityPipelineKeyProvider
{
    private readonly ConcurrentDictionary<(string scheme, string host, int port), string> _cache = new();

    public string GetPipelineKey(HttpRequestMessage requestMessage)
    {
        var url = requestMessage.RequestUri ?? throw new InvalidOperationException("The request message must have a URL specified.");

        var key = (url.Scheme, url.Host, url.Port);

        // We could use GetOrAdd for simplification but that would force us to allocate the lambda for every call.
        if (_cache.TryGetValue(key, out var pipelineKey))
        {
            return pipelineKey;
        }

        pipelineKey = url.GetLeftPart(UriPartial.Authority);

        // sometimes this can be called twice (multiple concurrent requests), but we don't care
        _cache[key] = pipelineKey!;

        return pipelineKey!;
    }
}
