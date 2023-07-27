// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Internal;

internal static class LogPropertyCollectorExtensions
{
    private static readonly ConcurrentDictionary<string, string> _requestPrefixedNamesCache = new();
    private static readonly ConcurrentDictionary<string, string> _responsePrefixedNamesCache = new();

    public static void AddRequestHeaders(this ILogPropertyCollector props, List<KeyValuePair<string, string>>? items)
    {
        if (items is not null)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var key = _requestPrefixedNamesCache.GetOrAdd(
                    items[i].Key,
                    static (x, p) => p + x,
                    HttpClientLoggingDimensions.RequestHeaderPrefix);

                props.Add(key, items[i].Value);
            }
        }
    }

    public static void AddResponseHeaders(this ILogPropertyCollector props, List<KeyValuePair<string, string>>? items)
    {
        if (items is not null)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var key = _responsePrefixedNamesCache.GetOrAdd(
                    items[i].Key,
                    static (x, p) => p + x,
                    HttpClientLoggingDimensions.ResponseHeaderPrefix);

                props.Add(key, items[i].Value);
            }
        }
    }
}
