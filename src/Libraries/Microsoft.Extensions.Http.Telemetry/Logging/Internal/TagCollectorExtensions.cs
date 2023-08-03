// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Internal;

internal static class TagCollectorExtensions
{
    private static readonly ConcurrentDictionary<string, string> _requestPrefixedNamesCache = new();
    private static readonly ConcurrentDictionary<string, string> _responsePrefixedNamesCache = new();

    public static void AddRequestHeaders(this ITagCollector tags, List<KeyValuePair<string, string>>? items)
    {
        if (items is not null)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var key = _requestPrefixedNamesCache.GetOrAdd(
                    items[i].Key,
                    static (x, p) => p + x,
                    HttpClientLoggingTagNames.RequestHeaderPrefix);
                tags.Add(key, items[i].Value);
            }
        }
    }

    public static void AddResponseHeaders(this ITagCollector collector, List<KeyValuePair<string, string>>? items)
    {
        if (items is not null)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var key = _responsePrefixedNamesCache.GetOrAdd(
                    items[i].Key,
                    static (x, p) => p + x,
                    HttpClientLoggingTagNames.ResponseHeaderPrefix);
                collector.Add(key, items[i].Value);
            }
        }
    }
}
