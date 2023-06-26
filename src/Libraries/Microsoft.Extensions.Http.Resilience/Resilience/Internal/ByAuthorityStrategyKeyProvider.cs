// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal sealed class ByAuthorityStrategyKeyProvider : IStrategyKeyProvider
{
    private readonly Redactor _redactor;
    private readonly ConcurrentDictionary<(string scheme, string host, int port), string> _cache = new();

    public ByAuthorityStrategyKeyProvider(Redactor redactor)
    {
        _redactor = redactor;
    }

    public string GetStrategyKey(HttpRequestMessage requestMessage)
    {
        var url = requestMessage.RequestUri ?? throw new InvalidOperationException("The request message must have a URL specified.");

        var key = (url.Scheme, url.Host, url.Port);

        // We could use GetOrAdd for simplification but that would force us to allocate the lambda for every call.
        if (_cache.TryGetValue(key, out var strategyKey))
        {
            return strategyKey;
        }

        strategyKey = url.GetLeftPart(UriPartial.Authority);
        strategyKey = _redactor.Redact(strategyKey);

        if (string.IsNullOrEmpty(strategyKey))
        {
            Throw.InvalidOperationException(
                "The redacted strategy key is an empty string and cannot be used for the strategy selection. Is redaction correctly configured?");
        }

        // sometimes this can be called twice (multiple concurrent requests), but we don't care
        _cache[key] = strategyKey!;

        return strategyKey!;
    }
}
